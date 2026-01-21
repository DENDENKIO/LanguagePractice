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

        /// <summary>
        /// AIの応答部分を抽出する（プロンプト部分を除外）
        /// </summary>
        private string ExtractAiResponse(string fullText)
        {
            string sentinel = LpConstants.DONE_SENTINEL;

            // プロンプト内のセンチネル数をスキップして、AI出力のセンチネルを探す
            int skipCount = _promptSentinelCount;
            int pos = 0;

            // プロンプト内のセンチネルをスキップ
            for (int i = 0; i < skipCount; i++)
            {
                pos = fullText.IndexOf(sentinel, pos, StringComparison.Ordinal);
                if (pos == -1) return fullText;
                pos += sentinel.Length;
            }

            // プロンプト部分の後から検索開始
            string afterPrompt = pos > 0 ? fullText.Substring(pos) : fullText;

            // AI出力内のセンチネルを探す
            int aiSentinelPos = afterPrompt.IndexOf(sentinel, StringComparison.Ordinal);
            if (aiSentinelPos == -1)
            {
                // センチネルが見つからない場合は全体を返す
                return afterPrompt;
            }

            // センチネルの前にあるマーカーを探す
            // 全てのマーカーをチェック
            int bestMarkerPos = -1;
            string bestMarkerName = "";

            foreach (var kvp in LpConstants.MarkerBegin)
            {
                string marker = kvp.Value;
                int markerPos = afterPrompt.IndexOf(marker, StringComparison.Ordinal);

                // マーカーがセンチネルより前にあり、最も後ろにあるマーカーを選ぶ
                // （AIの出力開始位置に最も近いマーカー）
                if (markerPos != -1 && markerPos < aiSentinelPos)
                {
                    if (markerPos > bestMarkerPos)
                    {
                        bestMarkerPos = markerPos;
                        bestMarkerName = marker;
                    }
                }
            }

            if (bestMarkerPos != -1)
            {
                // マーカーからセンチネルまでを抽出
                string extracted = afterPrompt.Substring(bestMarkerPos, aiSentinelPos + sentinel.Length - bestMarkerPos);
                return extracted;
            }

            // マーカーが見つからない場合、センチネルの手前の適切な範囲を返す
            // 最後のセンチネルを含む範囲を返す
            int extractStart = Math.Max(0, aiSentinelPos - 10000); // 最大10000文字前から
            return afterPrompt.Substring(extractStart, aiSentinelPos + sentinel.Length - extractStart);
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

  var input =
    document.querySelector('#ask-input[contenteditable=""true""][data-lexical-editor=""true""]') ||
    document.querySelector('#ask-input[contenteditable=""true""]') ||
    document.querySelector('div[contenteditable=""true""][role=""textbox""]') ||
    document.querySelector('textarea');

  if (!input) return 'INPUT_NOT_FOUND';

  input.focus();

  try {{
    var sel = window.getSelection();
    var range = document.createRange();
    range.selectNodeContents(input);
    sel.removeAllRanges();
    sel.addRange(range);
    document.execCommand('delete');
  }} catch(e) {{}}

  var ok = false;
  try {{
    ok = document.execCommand('insertText', false, prompt);
  }} catch(e) {{
    ok = false;
  }}

  if (!ok) {{
    try {{
      input.textContent = prompt;
      input.dispatchEvent(new InputEvent('input', {{ bubbles: true, data: prompt, inputType: 'insertText' }}));
    }} catch(e) {{}}
  }}

  setTimeout(() => {{
    var sendBtn =
      document.querySelector('button[aria-label=""送信""]') ||
      document.querySelector('button[aria-label*=""Send""]') ||
      document.querySelector('button[type=""submit""]');

    if (sendBtn) sendBtn.click();
    else {{
      input.dispatchEvent(new KeyboardEvent('keydown', {{ bubbles: true, key: 'Enter', code: 'Enter', keyCode: 13 }}));
    }}
  }}, 500);

  return 'INPUT_SET';
}})();
";
            }

            // ------- Genspark / その他は汎用 -------
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

                // ページ全体のセンチネル数をカウント
                int totalSentinelCount = CountSentinel(currentText);

                // プロンプト内のセンチネル数 + 1 = AIが出力したセンチネルが必要
                int requiredCount = _promptSentinelCount + 1;

                StatusMessage = $"監視中: Sentinel={totalSentinelCount}/{requiredCount}必要, 長さ={currentLength}, 安定={_stableCount}/{StableThreshold}";
                StatusText.Text = StatusMessage;

                // センチネルが必要数に達していない場合は待機
                if (totalSentinelCount < requiredCount)
                {
                    _stableCount = 0;
                    _lastTextLength = currentLength;
                    return;
                }

                // テキスト長が安定しているかチェック
                if (currentLength != _lastTextLength)
                {
                    _stableCount = 0;
                    _lastTextLength = currentLength;
                }
                else
                {
                    _stableCount++;
                }

                // 安定したら結果を取得
                if (_stableCount >= StableThreshold)
                {
                    _monitorTimer.Stop();
                    StatusText.Text = "生成完了を確認。2秒後に取得します...";

                    await Task.Delay(2000);

                    // 最終テキスト取得
                    rawJson = await webView.CoreWebView2.ExecuteScriptAsync(scriptGet);
                    string fullText = JsonSerializer.Deserialize<string>(rawJson) ?? "";

                    // AI応答部分を抽出
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

        private string StatusMessage = "";

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
