using LanguagePractice.Helpers;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LanguagePractice.Views
{
    public partial class BrowserWindow : Window
    {
        private readonly string _url;
        private readonly string _prompt;
        private readonly string _siteId;

        private bool _hasInjected = false;
        private readonly DispatcherTimer _monitorTimer;

        private int _lastTextLength = 0;
        private int _stableCount = 0;
        private const int StableThreshold = 5;

        private int _promptSentinelCount = 0;

        public string ResultText { get; private set; } = "";

        public BrowserWindow(string url, string prompt, string siteId = "GENSPARK")
        {
            InitializeComponent();
            _url = url;
            _prompt = prompt;
            _siteId = siteId;

            _promptSentinelCount = CountSentinel(_prompt);

            _monitorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _monitorTimer.Tick += MonitorTimer_Tick;

            InitializeBrowser();
        }

        private static int CountSentinel(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            string sentinel = LpConstants.DONE_SENTINEL;
            int count = 0;
            int pos = 0;
            while ((pos = text.IndexOf(sentinel, pos, StringComparison.Ordinal)) != -1)
            {
                count++;
                pos += sentinel.Length;
            }
            return count;
        }

        private string ExtractAiResponse(string fullText)
        {
            string sentinel = LpConstants.DONE_SENTINEL;

            int skipCount = _promptSentinelCount;
            int pos = 0;

            for (int i = 0; i < skipCount; i++)
            {
                pos = fullText.IndexOf(sentinel, pos, StringComparison.Ordinal);
                if (pos == -1) return fullText;
                pos += sentinel.Length;
            }

            string aiResponseStart = pos > 0 ? fullText.Substring(pos) : fullText;

            foreach (var marker in LpConstants.MarkerBegin.Values)
            {
                int beginPos = aiResponseStart.IndexOf(marker, StringComparison.Ordinal);
                if (beginPos != -1)
                {
                    int sentinelPos = aiResponseStart.IndexOf(sentinel, beginPos, StringComparison.Ordinal);
                    if (sentinelPos != -1)
                        return aiResponseStart.Substring(beginPos, sentinelPos + sentinel.Length - beginPos);
                }
            }

            int lastSentinelPos = aiResponseStart.LastIndexOf(sentinel, StringComparison.Ordinal);
            if (lastSentinelPos != -1)
            {
                int startPos = Math.Max(0, lastSentinelPos - 5000);
                return aiResponseStart.Substring(startPos, lastSentinelPos + sentinel.Length - startPos);
            }

            return aiResponseStart;
        }

        private async void InitializeBrowser()
        {
            StatusText.Text = "ブラウザ初期化中...";
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.NavigationCompleted += WebView_NavigationCompleted;
                StatusText.Text = "サイトへ移動中...";
                webView.CoreWebView2.Navigate(_url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2初期化エラー: {ex.Message}");
            }
        }

        private async void WebView_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                StatusText.Text = "読み込み失敗";
                return;
            }

            if (_hasInjected) return;

            StatusText.Text = "読み込み完了。3秒後に自動入力を試みます...";
            await Task.Delay(3000);
            await InjectPromptScript();
        }

        private static string JsEscape(string s)
        {
            return (s ?? "")
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "");
        }

        private string BuildInjectScript(string safePrompt)
        {
            // ------- 共通フォールバック（汎用）-------
            string generic = $@"
(function() {{
  function pickInput() {{
    return document.querySelector('textarea')
      || document.querySelector('div[contenteditable=""true""]')
      || document.querySelector('input[type=""text""]');
  }}

  var input = pickInput();
  if (!input) return 'INPUT_NOT_FOUND';

  input.focus();

  if (input.tagName === 'TEXTAREA' || input.tagName === 'INPUT') {{
    input.value = ""{safePrompt}"";
  }} else {{
    input.innerText = ""{safePrompt}"";
  }}

  input.dispatchEvent(new Event('input', {{ bubbles: true }}));

  setTimeout(() => {{
    var sendBtn =
      document.querySelector('button[aria-label=""送信""]') ||
      document.querySelector('button[type=""submit""]') ||
      document.querySelector('button[aria-label*=""Send""]') ||
      document.querySelector('button[aria-label*=""Submit""]');

    if (!sendBtn) {{
      var buttons = Array.from(document.querySelectorAll('button'));
      sendBtn = buttons.find(b =>
        (b.innerText && (b.innerText.toLowerCase().includes('send') || b.innerText.toLowerCase().includes('submit') || b.innerText.includes('送信'))) ||
        (b.getAttribute('aria-label') && (b.getAttribute('aria-label').toLowerCase().includes('send') || b.getAttribute('aria-label').toLowerCase().includes('submit')))
      );
    }}

    if (sendBtn) sendBtn.click();
    else {{
      input.dispatchEvent(new KeyboardEvent('keydown', {{ bubbles: true, key: 'Enter', code: 'Enter', keyCode: 13 }}));
    }}
  }}, 600);

  return 'INPUT_SET';
}})();
";

            // ------- Perplexity（Lexical editor対策）-------
            if (_siteId == "PERPLEXITY")
            {
                return $@"
(function() {{
  var prompt = ""{safePrompt}"";

  // あなたのHTMLに合わせて ask-input を最優先
  var input =
    document.querySelector('#ask-input[contenteditable=""true""][data-lexical-editor=""true""]') ||
    document.querySelector('#ask-input[contenteditable=""true""]') ||
    document.querySelector('div[contenteditable=""true""][role=""textbox""]') ||
    document.querySelector('textarea');

  if (!input) return 'INPUT_NOT_FOUND';

  // フォーカス
  input.focus();

  // 既存内容を全選択→削除（Lexicalはこの方が安全）
  try {{
    var sel = window.getSelection();
    var range = document.createRange();
    range.selectNodeContents(input);
    sel.removeAllRanges();
    sel.addRange(range);
    document.execCommand('delete');
  }} catch(e) {{}}

  // 文章挿入：Lexicalに効きやすい insertText を優先
  var ok = false;
  try {{
    ok = document.execCommand('insertText', false, prompt);
  }} catch(e) {{
    ok = false;
  }}

  // 失敗したらフォールバック（最悪でも表示はされる）
  if (!ok) {{
    try {{
      input.textContent = prompt;
      input.dispatchEvent(new InputEvent('input', {{ bubbles: true, data: prompt, inputType: 'insertText' }}));
    }} catch(e) {{}}
  }}

  // 送信ボタン（aria-label=送信 を最優先）
  setTimeout(() => {{
    var sendBtn =
      document.querySelector('button[aria-label=""送信""]') ||
      document.querySelector('button[aria-label*=""Send""]') ||
      document.querySelector('button[type=""submit""]');

    if (sendBtn) sendBtn.click();
    else {{
      // Enter送信（Perplexityは基本Enter送信）
      input.dispatchEvent(new KeyboardEvent('keydown', {{ bubbles: true, key: 'Enter', code: 'Enter', keyCode: 13 }}));
    }}
  }}, 500);

  return 'INPUT_SET';
}})();
";
            }

            // ------- Genspark（今まで通り汎用でOK）-------
            if (_siteId == "GENSPARK")
                return generic;

            // ------- その他は汎用 -------
            return generic;
        }

        private async Task InjectPromptScript()
        {
            StatusText.Text = "自動入力実行中...";

            string safePrompt = JsEscape(_prompt);
            string script = BuildInjectScript(safePrompt);

            try
            {
                string result = await webView.CoreWebView2.ExecuteScriptAsync(script);
                if (result.Contains("INPUT_NOT_FOUND"))
                {
                    StatusText.Text = "入力欄が見つかりません。手動で入力してください。";
                }
                else
                {
                    StatusText.Text = "入力完了。生成完了を待機中...";
                    _hasInjected = true;
                    _lastTextLength = 0;
                    _stableCount = 0;
                    _monitorTimer.Start();
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Script Error: {ex.Message}";
            }
        }

        private async void MonitorTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                string scriptGet = "document.body.innerText";
                string rawJson = await webView.CoreWebView2.ExecuteScriptAsync(scriptGet);
                string currentText = JsonSerializer.Deserialize<string>(rawJson) ?? "";

                int currentLength = currentText.Length;

                int totalSentinelCount = CountSentinel(currentText);
                int requiredCount = _promptSentinelCount + 1;

                StatusText.Text = $"監視中: Sentinel={totalSentinelCount}/{requiredCount}必要, 長さ={currentLength}, 安定={_stableCount}/{StableThreshold}";

                if (totalSentinelCount < requiredCount)
                {
                    _stableCount = 0;
                    _lastTextLength = currentLength;
                    return;
                }

                if (currentLength != _lastTextLength)
                {
                    _stableCount = 0;
                    _lastTextLength = currentLength;
                }
                else
                {
                    _stableCount++;
                }

                if (_stableCount >= StableThreshold)
                {
                    _monitorTimer.Stop();
                    StatusText.Text = "生成完了を確認。2秒後に取得します...";

                    await Task.Delay(2000);

                    rawJson = await webView.CoreWebView2.ExecuteScriptAsync(scriptGet);
                    string fullText = JsonSerializer.Deserialize<string>(rawJson) ?? "";

                    ResultText = ExtractAiResponse(fullText);

                    DialogResult = true;
                    Close();
                }
            }
            catch
            {
                // 無視して継続
            }
        }

        private async void ManualInject_Click(object sender, RoutedEventArgs e)
        {
            await InjectPromptScript();
        }

        private void CopyAndClose_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                ResultText = Clipboard.GetText();
            }
            DialogResult = true;
            Close();
        }
    }
}
