using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    public class RouteExecutionViewModel : ViewModelBase
    {
        private readonly PromptBuilder _promptBuilder;
        private readonly LpExecutionContext _context;

        public RouteDefinition Route { get; }
        public ObservableCollection<RouteStepViewModel> Steps { get; }
        public RunPanelViewModel RunPanelVM { get; }

        private int _currentStepIndex = 0;
        private RouteStep? _currentStep;

        public string RouteTitle => $"実行中: {Route.Title}";

        public RouteExecutionViewModel(RouteDefinition route)
        {
            Route = route;
            _promptBuilder = new PromptBuilder();
            _context = new LpExecutionContext();
            RunPanelVM = new RunPanelViewModel();

            Steps = new ObservableCollection<RouteStepViewModel>(
                route.Steps.Select(s => new RouteStepViewModel(s))
            );

            if (route.Steps.Count > 0)
            {
                StartStep(0);
            }
            else
            {
                MessageBox.Show("このルートにはステップが含まれていません。編集画面でステップを追加してください。",
                    "実行不可", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartStep(int index)
        {
            if (index >= Route.Steps.Count)
            {
                MessageBox.Show("ルート完了！すべてのステップが終了しました。",
                    "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _currentStepIndex = index;
            _currentStep = Route.Steps[index];

            foreach (var s in Steps) s.IsActive = false;
            Steps[index].IsActive = true;

            string prompt = BuildPromptForStep(_currentStep);
            long? prevWorkId = _context.GetPreviousWorkId();

            RunPanelVM.Setup(_currentStep.Operation, prompt, prevWorkId);
        }

        private string BuildPromptForStep(RouteStep step)
        {
            // ---- InputBindings 解決 ----
            // "PREV_OUTPUT:KEY" / "FIXED:..." / "MANUAL" を解釈しつつ、
            // 何も無ければ過去出力から拾うフォールバックも入れる。
            string Resolve(string key, string manualHint = "")
            {
                if (step.InputBindings != null && step.InputBindings.TryGetValue(key, out var binding))
                {
                    if (binding == null) return "";

                    if (binding.StartsWith("PREV_OUTPUT:"))
                    {
                        string targetKey = binding.Substring("PREV_OUTPUT:".Length);
                        return _context.GetPreviousOutputValue(targetKey);
                    }
                    if (binding.StartsWith("FIXED:"))
                    {
                        return binding.Substring("FIXED:".Length);
                    }
                    if (binding.Trim().Equals("MANUAL", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // プロンプトはReadOnlyなので、ユーザーがAIサイト側で置換しやすい目印を入れる
                        return $"[MANUAL_INPUT_REQUIRED:{key}{(string.IsNullOrWhiteSpace(manualHint) ? "" : ":" + manualHint)}]";
                    }
                }

                // バインディングが無ければ過去出力から拾う（route editorのMVP仕様を補助）
                return _context.GetPreviousOutputValue(key);
            }

            string ResolveAny(params string[] keys)
            {
                foreach (var k in keys)
                {
                    var v = Resolve(k);
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
                return "";
            }

            // ---- GIKO用：トーンルールの簡易プリセット ----
            // RouteEditorのGUIではToneRuleTextを設定できないため、FixedToneから雑に推定できるようにする
            string GetToneRulePreset(string toneLabel)
            {
                toneLabel = (toneLabel ?? "").Trim();

                if (toneLabel.Contains("平安") || toneLabel.Contains("王朝"))
                {
                    return
@"- 係り結びを時々使う（ぞ/なむ/や/か → 連体形、こそ → 已然形）
- 終止は「〜なり」「〜けり」「〜たり」を混ぜる
- 指示語は「この/その」より「かく/さ/それ」寄り
- 語尾は柔らかく、余韻を残す";
                }
                if (toneLabel.Contains("漢文") || toneLabel.Contains("訓読"))
                {
                    return
@"- 漢語を増やし、簡潔に切る
- 文末は「〜なり」「〜たり」「〜せり」等で整える
- 語順はやや硬く、説明を削る
- 難語は最小限のルビを許す";
                }
                if (toneLabel.Contains("江戸") || toneLabel.Contains("戯作"))
                {
                    return
@"- 口語的な調子と地の文を混ぜる（ただし現代スラングは避ける）
- ツッコミ/言いさし/間を作る
- 擬音語や言い回しでテンポを作る";
                }

                // デフォルト（汎用の「古典調」）
                return
@"- 意味と情景は変えず、文体だけ古典寄りに
- 語尾は「〜なり」「〜けり」等を混ぜる
- ルビは難読語のみ最小限";
            }

            // ---- Operationごとのプロンプト生成 ----
            switch (step.Operation)
            {
                case OperationKind.TEXT_GEN:
                    {
                        string topic = Resolve("TOPIC");
                        string reader = Resolve("READER");
                        string writer = Resolve("WRITER");
                        LengthProfile length = step.FixedLength ?? LengthProfile.STUDY_SHORT;

                        return _promptBuilder.BuildTextGenPrompt(writer, topic, reader, step.FixedTone ?? "", length);
                    }

                case OperationKind.STUDY_CARD:
                    {
                        string sourceText = ResolveAny("SOURCE_TEXT", "TEXT", "INPUT_TEXT");
                        string reader = Resolve("READER");
                        return _promptBuilder.BuildStudyCardPrompt(reader, step.FixedTone ?? "", sourceText);
                    }

                case OperationKind.TOPIC_GEN:
                    {
                        // 画像URLは任意。無ければ空でOK（プロンプト側で「なし」扱いになる）
                        string imageUrl = Resolve("IMAGE_URL", "https://example.com/image.jpg");
                        // MANUALのままでもOK（置換用目印が入る）
                        return _promptBuilder.BuildTopicGenPrompt(imageUrl);
                    }

                case OperationKind.PERSONA_GEN:
                    {
                        string genre = ResolveAny("GENRE", "CONTEXT", "THEME");
                        return _promptBuilder.BuildPersonaGenPrompt(genre);
                    }

                case OperationKind.OBSERVE_IMAGE:
                    {
                        string imageUrl = Resolve("IMAGE_URL", "https://example.com/image.jpg");
                        return _promptBuilder.BuildObserveImagePrompt(imageUrl);
                    }

                case OperationKind.CORE_EXTRACT:
                    {
                        string reader = Resolve("READER");
                        string sourceText = ResolveAny("SOURCE_TEXT", "TEXT", "INPUT_TEXT");
                        return _promptBuilder.BuildCoreExtractPrompt(reader, sourceText);
                    }

                case OperationKind.REVISION_FULL:
                    {
                        // 必須：sourceText / coreSentence（足りない場合もMANUAL目印で通す）
                        string sourceText = ResolveAny("SOURCE_TEXT", "TEXT", "INPUT_TEXT");
                        if (string.IsNullOrWhiteSpace(sourceText))
                            sourceText = "[MANUAL_INPUT_REQUIRED:SOURCE_TEXT]";

                        string coreTheme = Resolve("CORE_THEME");
                        string coreEmotion = Resolve("CORE_EMOTION");
                        string coreTakeaway = Resolve("CORE_TAKEAWAY");
                        string coreReader = Resolve("READER");
                        string coreSentence = Resolve("CORE_SENTENCE");
                        if (string.IsNullOrWhiteSpace(coreSentence))
                            coreSentence = "[MANUAL_INPUT_REQUIRED:CORE_SENTENCE]";

                        return _promptBuilder.BuildRevisionFullPrompt(
                            sourceText,
                            coreTheme,
                            coreEmotion,
                            coreTakeaway,
                            coreReader,
                            coreSentence
                        );
                    }

                case OperationKind.GIKO:
                    {
                        // ルートから渡したいキー：
                        // SOURCE_TEXT / TOPIC / READER / （tone label は step.FixedTone 推奨）/ TONE_RULE（任意）
                        string inputBody = ResolveAny("SOURCE_TEXT", "TEXT", "INPUT_TEXT");
                        if (string.IsNullOrWhiteSpace(inputBody))
                            inputBody = "[MANUAL_INPUT_REQUIRED:SOURCE_TEXT]";

                        string reader = Resolve("READER");
                        string topic = Resolve("TOPIC");
                        string toneLabel = !string.IsNullOrWhiteSpace(step.FixedTone)
                            ? step.FixedTone
                            : ResolveAny("TONE", "TONE_LABEL");

                        if (string.IsNullOrWhiteSpace(toneLabel))
                            toneLabel = "古典調（指定なし）";

                        string toneRule = Resolve("TONE_RULE");
                        if (string.IsNullOrWhiteSpace(toneRule))
                            toneRule = GetToneRulePreset(toneLabel);

                        return _promptBuilder.BuildGikoPrompt(toneLabel, toneRule, reader, topic, inputBody);
                    }

                case OperationKind.PERSONA_VERIFY_ASSIST:
                    {
                        // ルートから渡したいキー：
                        // PERSONA_NAME / PERSONA_BIO / EVIDENCE1..3
                        string name = ResolveAny("PERSONA_NAME", "NAME");
                        if (string.IsNullOrWhiteSpace(name)) name = "[MANUAL_INPUT_REQUIRED:PERSONA_NAME]";

                        string bio = ResolveAny("PERSONA_BIO", "BIO");
                        if (string.IsNullOrWhiteSpace(bio)) bio = "[MANUAL_INPUT_REQUIRED:PERSONA_BIO]";

                        string e1 = Resolve("EVIDENCE1");
                        string e2 = Resolve("EVIDENCE2");
                        string e3 = Resolve("EVIDENCE3");

                        return _promptBuilder.BuildPersonaVerifyPrompt(name, bio, e1, e2, e3);
                    }

                case OperationKind.READER_AUTO_GEN:
                    {
                        string contextKind = ResolveAny("CONTEXT_KIND", "KIND", "GENRE");
                        if (string.IsNullOrWhiteSpace(contextKind)) contextKind = "TEXT_GEN";
                        return _promptBuilder.BuildReaderAutoPrompt(contextKind);
                    }

                // ルート実行での意味が薄いものはメッセージ（必要なら後で実装）
                case OperationKind.PRACTICE_SESSION:
                    return "PRACTICE_SESSION はルート実行では未対応です（練習画面から実行してください）。";

                default:
                    return $"【{step.Operation}】\n（このOperationのプロンプト生成は未実装です）";
            }
        }

        private ICommand? _nextStepCommand;
        public ICommand NextStepCommand => _nextStepCommand ??= new RelayCommand(ExecuteNextStep);

        private void ExecuteNextStep()
        {
            var output = RunPanelVM.GetLastParsedOutput();

            if (output == null || output.Count == 0)
            {
                if (MessageBox.Show("まだ解析結果がありませんが、次のステップへ進みますか？\n（次ステップへのデータ引き継ぎができない可能性があります）",
                    "確認", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }

            long? savedId = RunPanelVM.GetLastSavedId();
            _context.AddOutput(_currentStep!.StepNumber, output ?? new System.Collections.Generic.Dictionary<string, string>(), savedId);

            Steps[_currentStepIndex].IsCompleted = true;
            Steps[_currentStepIndex].IsActive = false;

            StartStep(_currentStepIndex + 1);
        }
    }

    public class RouteStepViewModel : ViewModelBase
    {
        public RouteStep Step { get; }
        public string Title => Step.Title;
        public string OperationName => Step.Operation.ToString();

        private bool _isActive;
        public bool IsActive { get => _isActive; set { _isActive = value; OnPropertyChanged(); } }

        private bool _isCompleted;
        public bool IsCompleted { get => _isCompleted; set { _isCompleted = value; OnPropertyChanged(); } }

        public RouteStepViewModel(RouteStep step)
        {
            Step = step;
        }
    }
}
