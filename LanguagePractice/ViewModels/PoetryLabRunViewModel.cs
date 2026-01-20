using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using LanguagePractice.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    /// <summary>
    /// Runステップの表示用モデル
    /// </summary>
    public class RunStepItem : ViewModelBase
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsHumanStep { get; set; }

        private string _status = "PENDING";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusIcon)); }
        }

        public string StatusIcon => Status switch
        {
            "SUCCESS" => "✓",
            "FAILED" => "✗",
            "RUNNING" => "●",
            "WAITING" => "●",
            "SKIPPED" => "−",
            _ => "○"
        };

        public int? StepLogId { get; set; }
    }

    /// <summary>
    /// Core候補
    /// </summary>
    public class CoreCandidate : ViewModelBase
    {
        public int Index { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Emotion { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Oneline { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
    }

    /// <summary>
    /// PoetryLab Run実行画面 ViewModel
    /// </summary>
    public class PoetryLabRunViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly PoetryDatabaseService _db;
        private readonly PoetryRunService _runService;
        private readonly PoetryPromptBuilder _promptBuilder;
        private readonly PoetryOutputParser _outputParser;
        private readonly PoetryLabSettingsReader _settingsReader;
        private readonly int _projectId;
        private readonly int _runId;

        // 自動モード用：BrowserWindow参照
        private BrowserWindow? _browserWindow;
        private TaskCompletionSource<string>? _autoModeCompletionSource;

        #region Properties

        private PlProject? _project;
        public PlProject? Project
        {
            get => _project;
            set { _project = value; OnPropertyChanged(); }
        }

        private PlRun? _run;
        public PlRun? Run
        {
            get => _run;
            set { _run = value; OnPropertyChanged(); }
        }

        private ObservableCollection<RunStepItem> _steps = new();
        public ObservableCollection<RunStepItem> Steps
        {
            get => _steps;
            set { _steps = value; OnPropertyChanged(); }
        }

        private RunStepItem? _currentStep;
        public RunStepItem? CurrentStep
        {
            get => _currentStep;
            set
            {
                _currentStep = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHumanStepActive));
                OnPropertyChanged(nameof(IsCoreAdoptStep));
                OnPropertyChanged(nameof(IsWinnerSelectStep));
                OnPropertyChanged(nameof(IsAiStep));
                OnPropertyChanged(nameof(IsExportStep));
            }
        }

        public bool IsHumanStepActive => CurrentStep?.IsHumanStep == true;
        public bool IsCoreAdoptStep => CurrentStep?.Name == "CORE_ADOPT";
        public bool IsWinnerSelectStep => CurrentStep?.Name == "WINNER_SELECT";
        public bool IsAiStep => CurrentStep != null && !CurrentStep.IsHumanStep && CurrentStep.Name != "EXPORT";
        public bool IsExportStep => CurrentStep?.Name == "EXPORT";

        private string _currentPrompt = string.Empty;
        public string CurrentPrompt
        {
            get => _currentPrompt;
            set { _currentPrompt = value; OnPropertyChanged(); }
        }

        private string _manualInput = string.Empty;
        public string ManualInput
        {
            get => _manualInput;
            set { _manualInput = value; OnPropertyChanged(); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        private bool _isManualMode;
        public bool IsManualMode
        {
            get => _isManualMode;
            set { _isManualMode = value; OnPropertyChanged(); }
        }

        private bool _isAutoMode;
        public bool IsAutoMode
        {
            get => _isAutoMode;
            set { _isAutoMode = value; OnPropertyChanged(); }
        }

        // Core採択用
        private ObservableCollection<CoreCandidate> _coreCandidates = new();
        public ObservableCollection<CoreCandidate> CoreCandidates
        {
            get => _coreCandidates;
            set { _coreCandidates = value; OnPropertyChanged(); }
        }

        private bool _useCustomCore;
        public bool UseCustomCore
        {
            get => _useCustomCore;
            set
            {
                _useCustomCore = value;
                OnPropertyChanged();
                if (value)
                {
                    foreach (var c in CoreCandidates)
                    {
                        c.IsSelected = false;
                    }
                }
            }
        }

        private string _customCoreSubject = string.Empty;
        public string CustomCoreSubject
        {
            get => _customCoreSubject;
            set { _customCoreSubject = value; OnPropertyChanged(); }
        }

        private string _customCoreEmotion = string.Empty;
        public string CustomCoreEmotion
        {
            get => _customCoreEmotion;
            set { _customCoreEmotion = value; OnPropertyChanged(); }
        }

        private string _customCoreQuestion = string.Empty;
        public string CustomCoreQuestion
        {
            get => _customCoreQuestion;
            set { _customCoreQuestion = value; OnPropertyChanged(); }
        }

        private string _customCoreOneline = string.Empty;
        public string CustomCoreOneline
        {
            get => _customCoreOneline;
            set { _customCoreOneline = value; OnPropertyChanged(); }
        }

        private string _previousStepResult = string.Empty;
        public string PreviousStepResult
        {
            get => _previousStepResult;
            set { _previousStepResult = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand BackCommand { get; }
        public ICommand CancelRunCommand { get; }
        public ICommand ExecuteStepCommand { get; }
        public ICommand ExecuteAutoCommand { get; }
        public ICommand RetryStepCommand { get; }
        public ICommand SubmitManualInputCommand { get; }
        public ICommand CopyPromptCommand { get; }
        public ICommand AdoptCoreCommand { get; }
        public ICommand ProceedToCompareCommand { get; }
        public ICommand ViewPreviousResultCommand { get; }
        public ICommand SwitchToManualCommand { get; }

        #endregion

        #region Constructor

        public PoetryLabRunViewModel(MainViewModel mainViewModel, PoetryDatabaseService db, int projectId, int runId)
        {
            _mainViewModel = mainViewModel;
            _db = db;
            _projectId = projectId;
            _runId = runId;
            _runService = new PoetryRunService(_db);
            _promptBuilder = new PoetryPromptBuilder();
            _outputParser = new PoetryOutputParser();
            _settingsReader = new PoetryLabSettingsReader();

            // コマンド初期化
            BackCommand = new RelayCommand(GoBack);
            CancelRunCommand = new RelayCommand(CancelRun);
            ExecuteStepCommand = new RelayCommand(ExecuteCurrentStep);
            ExecuteAutoCommand = new RelayCommand(async () => await ExecuteAutoModeAsync());
            RetryStepCommand = new RelayCommand(RetryStep);
            SubmitManualInputCommand = new RelayCommand(SubmitManualInput);
            CopyPromptCommand = new RelayCommand(CopyPrompt);
            AdoptCoreCommand = new RelayCommand(AdoptCore, CanAdoptCore);
            ProceedToCompareCommand = new RelayCommand(ProceedToCompare);
            ViewPreviousResultCommand = new RelayCommand(ViewPreviousResult);
            SwitchToManualCommand = new RelayCommand(SwitchToManual);

            // 自動モード設定確認
            IsAutoMode = _settingsReader.IsAutoMode();

            // データ読み込み
            LoadData();
            InitializeSteps();
        }

        #endregion

        #region Initialization

        private void LoadData()
        {
            Project = _db.GetProject(_projectId);
            Run = _db.GetRun(_runId);
        }

        private void InitializeSteps()
        {
            var definitions = PoetryStepDefinition.StandardRun;
            var stepLogs = _runService.GetStepLogs(_runId);

            Steps = new ObservableCollection<RunStepItem>(
                definitions.Select(d =>
                {
                    var log = stepLogs.FirstOrDefault(l => l.StepName == d.Name);
                    return new RunStepItem
                    {
                        Index = d.Index,
                        Name = d.Name,
                        DisplayName = d.DisplayName,
                        IsHumanStep = d.IsHumanStep,
                        Status = log?.Status ?? "PENDING",
                        StepLogId = log?.Id
                    };
                })
            );

            CurrentStep = Steps.FirstOrDefault(s =>
                s.Status == "PENDING" ||
                s.Status == "RUNNING" ||
                s.Status == "WAITING" ||
                s.Status == "FAILED");

            if (CurrentStep == null)
            {
                CurrentStep = Steps.LastOrDefault();
            }

            if (CurrentStep != null)
            {
                PrepareStep(CurrentStep);
            }
        }

        #endregion

        #region Step Preparation

        private void PrepareStep(RunStepItem step)
        {
            StatusMessage = $"ステップ {step.Index}: {step.DisplayName}";
            IsManualMode = false;
            ManualInput = string.Empty;

            if (step.IsHumanStep)
            {
                step.Status = "WAITING";

                if (step.Name == "CORE_ADOPT")
                {
                    LoadCoreCandidates();
                }
            }
            else if (step.Name == "EXPORT")
            {
                StatusMessage = "エクスポート準備完了";
            }
            else
            {
                var inputs = GatherInputs(step);
                CurrentPrompt = _promptBuilder.Build(step.Name, inputs, Project?.StyleType ?? "KOU");
            }

            LoadPreviousStepResult(step);
        }

        private void LoadPreviousStepResult(RunStepItem currentStep)
        {
            if (currentStep.Index <= 1)
            {
                PreviousStepResult = "(前ステップなし)";
                return;
            }

            var prevStep = Steps.FirstOrDefault(s => s.Index == currentStep.Index - 1);
            if (prevStep == null)
            {
                PreviousStepResult = "(前ステップなし)";
                return;
            }

            var definition = PoetryStepDefinition.StandardRun.FirstOrDefault(d => d.Name == prevStep.Name);
            if (definition == null || definition.OutputKeys.Length == 0)
            {
                PreviousStepResult = "(出力なし)";
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var outputKey in definition.OutputKeys)
            {
                var asset = _db.GetTextAssetByType(_runId, outputKey);
                if (asset != null)
                {
                    sb.AppendLine($"=== {outputKey} ===");
                    sb.AppendLine(asset.BodyText);
                    sb.AppendLine();
                }
            }

            PreviousStepResult = sb.Length > 0 ? sb.ToString() : "(出力なし)";
        }

        private Dictionary<string, string> GatherInputs(RunStepItem step)
        {
            var definition = PoetryStepDefinition.StandardRun.First(d => d.Name == step.Name);
            var inputs = new Dictionary<string, string>();

            foreach (var inputKey in definition.InputKeys)
            {
                var asset = _db.GetTextAssetByType(_runId, inputKey);
                if (asset != null)
                {
                    inputs[inputKey] = asset.BodyText;
                }
            }

            return inputs;
        }

        #endregion

        #region Auto Mode Execution

        /// <summary>
        /// 自動モードでステップを実行
        /// </summary>
        private async Task ExecuteAutoModeAsync()
        {
            if (CurrentStep == null || IsRunning) return;
            if (CurrentStep.IsHumanStep || CurrentStep.Name == "EXPORT") return;

            IsRunning = true;
            CurrentStep.Status = "RUNNING";
            StatusMessage = $"自動実行中: {CurrentStep.DisplayName}";

            try
            {
                // StepLog作成
                var inputs = GatherInputs(CurrentStep);
                var inputKeysJson = JsonSerializer.Serialize(inputs.Keys.ToList());
                var stepLogId = _db.CreateAiStepLog(_runId, CurrentStep.Index, CurrentStep.Name, inputKeysJson, CurrentPrompt);
                CurrentStep.StepLogId = stepLogId;
                _db.UpdateAiStepLogStatus(stepLogId, "RUNNING");

                // AI URL取得
                var aiUrl = _settingsReader.GetAiUrl();
                if (string.IsNullOrEmpty(aiUrl))
                {
                    StatusMessage = "AI URLが設定されていません。手動モードに切り替えます。";
                    IsManualMode = true;
                    IsRunning = false;
                    return;
                }

                // BrowserWindowを開いて自動実行
                var result = await ExecuteWithBrowserAsync(aiUrl, CurrentPrompt);

                if (result.Success)
                {
                    // パース
                    var expectedSection = GetExpectedSection(CurrentStep.Name);
                    PoetryParseResult parseResult;

                    if (CurrentStep.Name == "POEM_REVISION_GEN")
                    {
                        parseResult = _outputParser.ParseRevisions(result.Output);
                    }
                    else
                    {
                        parseResult = _outputParser.Parse(result.Output, expectedSection);
                    }

                    if (!parseResult.Success)
                    {
                        StatusMessage = $"パースエラー: {parseResult.ErrorMessage}。手動モードに切り替えます。";
                        ManualInput = result.Output; // 取得した出力を手動入力欄にセット
                        IsManualMode = true;
                        IsRunning = false;
                        return;
                    }

                    // 保存
                    var parsedJson = JsonSerializer.Serialize(parseResult.Data);
                    _db.UpdateAiStepLogResult(stepLogId, result.Output, parsedJson, "SUCCESS");
                    SaveAssets(CurrentStep, parseResult);

                    if (CurrentStep.Name == "POEM_ISSUE_GEN")
                    {
                        SaveIssues(stepLogId);
                    }

                    CurrentStep.Status = "SUCCESS";
                    StatusMessage = $"完了: {CurrentStep.DisplayName}";

                    // 次のステップへ
                    MoveToNextStep();
                }
                else
                {
                    StatusMessage = $"自動実行失敗: {result.ErrorMessage}。手動モードに切り替えます。";
                    IsManualMode = true;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}。手動モードに切り替えます。";
                IsManualMode = true;
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// BrowserWindowを使用してAIに問い合わせ
        /// </summary>
        private async Task<(bool Success, string Output, string? ErrorMessage)> ExecuteWithBrowserAsync(string aiUrl, string prompt)
        {
            try
            {
                _autoModeCompletionSource = new TaskCompletionSource<string>();

                var siteId = _settingsReader.GetAiSiteId();

                // BrowserWindow を生成
                _browserWindow = new BrowserWindow(aiUrl, prompt, siteId);

                // モーダル表示し、ユーザー操作または内部処理で閉じられるのを待つ
                var dialogResult = _browserWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var output = _browserWindow.ResultText ?? string.Empty;
                    return (true, output, null);
                }
                else
                {
                    return (false, string.Empty, "ブラウザでの実行がキャンセルされました");
                }
            }
            catch (Exception ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }

        /// <summary>
        /// 手動モードに切り替え
        /// </summary>
        private void SwitchToManual()
        {
            IsManualMode = true;
            StatusMessage = "手動モードに切り替えました。プロンプトをコピーしてAIに投入してください。";
        }

        #endregion

        #region Manual Mode Execution

        private void ExecuteCurrentStep()
        {
            if (CurrentStep == null || IsRunning) return;

            if (CurrentStep.Name == "EXPORT")
            {
                ExecuteExport();
                return;
            }

            if (CurrentStep.IsHumanStep) return;

            // 自動モードの場合は自動実行
            if (IsAutoMode && !IsManualMode)
            {
                _ = ExecuteAutoModeAsync();
                return;
            }

            // 手動モード
            IsRunning = true;
            CurrentStep.Status = "RUNNING";
            StatusMessage = $"準備中: {CurrentStep.DisplayName}";

            try
            {
                // StepLog作成
                var inputs = GatherInputs(CurrentStep);
                var inputKeysJson = JsonSerializer.Serialize(inputs.Keys.ToList());
                var stepLogId = _db.CreateAiStepLog(_runId, CurrentStep.Index, CurrentStep.Name, inputKeysJson, CurrentPrompt);
                CurrentStep.StepLogId = stepLogId;
                _db.UpdateAiStepLogStatus(stepLogId, "RUNNING");

                StatusMessage = "手動モード: プロンプトをコピーしてAIに投入し、結果を貼り付けてください";
                IsManualMode = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                CurrentStep.Status = "FAILED";
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void SubmitManualInput()
        {
            if (CurrentStep == null || string.IsNullOrWhiteSpace(ManualInput)) return;

            var stepLogId = CurrentStep.StepLogId;
            if (stepLogId == null)
            {
                StatusMessage = "StepLogが見つかりません。再実行してください。";
                return;
            }

            try
            {
                var expectedSection = GetExpectedSection(CurrentStep.Name);
                PoetryParseResult parseResult;

                if (CurrentStep.Name == "POEM_REVISION_GEN")
                {
                    parseResult = _outputParser.ParseRevisions(ManualInput);
                }
                else
                {
                    parseResult = _outputParser.Parse(ManualInput, expectedSection);
                }

                if (!parseResult.Success)
                {
                    StatusMessage = $"パースエラー: {parseResult.ErrorMessage}";
                    return;
                }

                var parsedJson = JsonSerializer.Serialize(parseResult.Data);
                _db.UpdateAiStepLogResult(stepLogId.Value, ManualInput, parsedJson, "SUCCESS");
                SaveAssets(CurrentStep, parseResult);

                if (CurrentStep.Name == "POEM_ISSUE_GEN")
                {
                    SaveIssues(stepLogId.Value);
                }

                CurrentStep.Status = "SUCCESS";
                StatusMessage = $"完了: {CurrentStep.DisplayName}";

                ManualInput = string.Empty;
                IsManualMode = false;
                MoveToNextStep();
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
            }
        }

        private string GetExpectedSection(string stepName)
        {
            return stepName switch
            {
                "POEM_TOPIC_GEN" => "TOPIC",
                "POEM_DRAFT_GEN" => "DRAFT",
                "POEM_CORE_EXTRACT" => "CORE_CANDIDATES",
                "POEM_LINE_MAP" => "LINE_MAP",
                "POEM_ISSUE_GEN" => "ISSUES",
                "POEM_DIAGNOSE_GEN" => "DIAGNOSES",
                "POEM_REVISION_GEN" => "REVISIONS",
                _ => stepName
            };
        }

        private void SaveAssets(RunStepItem step, PoetryParseResult parseResult)
        {
            var definition = PoetryStepDefinition.StandardRun.First(d => d.Name == step.Name);
            var inputKeysJson = JsonSerializer.Serialize(definition.InputKeys);

            foreach (var outputKey in definition.OutputKeys)
            {
                string bodyText;

                if (step.Name == "POEM_REVISION_GEN")
                {
                    var bodyKey = $"{outputKey}_BODY";
                    var approachKey = $"{outputKey}_APPROACH";
                    var approach = parseResult.Data.GetValueOrDefault(approachKey, "");
                    var body = parseResult.Data.GetValueOrDefault(bodyKey, "");
                    bodyText = $"[Approach: {approach}]\n\n{body}";
                }
                else if (step.Name == "POEM_TOPIC_GEN")
                {
                    var theme = parseResult.Data.GetValueOrDefault("THEME", "");
                    var constraint = parseResult.Data.GetValueOrDefault("CONSTRAINT", "");
                    var seedImage = parseResult.Data.GetValueOrDefault("SEED_IMAGE", "");
                    bodyText = $"THEME: {theme}\nCONSTRAINT: {constraint}\nSEED_IMAGE: {seedImage}";
                }
                else if (step.Name == "POEM_DRAFT_GEN")
                {
                    bodyText = parseResult.Data.GetValueOrDefault("BODY", parseResult.RawSection);
                }
                else
                {
                    bodyText = parseResult.RawSection;
                }

                _db.CreateTextAsset(_projectId, _runId, step.StepLogId, outputKey, inputKeysJson, bodyText);
            }
        }

        private void SaveIssues(int stepLogId)
        {
            var stepLog = _db.GetAiStepLog(stepLogId);
            if (stepLog?.RawOutput == null) return;

            var issues = _outputParser.ParseIssues(stepLog.RawOutput, _projectId, _runId, stepLogId);
            foreach (var issue in issues)
            {
                _db.CreateIssue(issue);
            }
        }

        private void ExecuteExport()
        {
            try
            {
                var exportService = new PoetryExportService(_db);

                var run = _db.GetRun(_runId);
                if (run == null)
                {
                    StatusMessage = "Runが見つかりません";
                    return;
                }

                if (run.Status != "SUCCESS")
                {
                    _runService.Complete(_runId);
                }

                var result = exportService.Export(_projectId, _runId);

                if (result.Success)
                {
                    CurrentStep!.Status = "SUCCESS";
                    StatusMessage = $"エクスポート完了: {result.FilePath}";

                    MessageBox.Show(
                        $"エクスポートが完了しました。\n\n保存先:\n{result.FilePath}",
                        "PoetryLab",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    StatusMessage = $"エクスポート失敗: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"エクスポートエラー: {ex.Message}";
            }
        }

        #endregion

        #region Core Adoption

        private void LoadCoreCandidates()
        {
            var asset = _db.GetTextAssetByType(_runId, "CORE_CANDIDATES");
            if (asset == null)
            {
                StatusMessage = "Core候補が見つかりません";
                return;
            }

            var candidates = new List<CoreCandidate>();
            var lines = asset.BodyText.Split('\n');

            for (int i = 1; i <= 3; i++)
            {
                var subject = ExtractValue(lines, $"CANDIDATE_{i}_SUBJECT");
                var emotion = ExtractValue(lines, $"CANDIDATE_{i}_EMOTION");
                var question = ExtractValue(lines, $"CANDIDATE_{i}_QUESTION");
                var oneline = ExtractValue(lines, $"ONELINE_{i}");

                if (!string.IsNullOrEmpty(subject))
                {
                    candidates.Add(new CoreCandidate
                    {
                        Index = i,
                        Subject = subject,
                        Emotion = emotion,
                        Question = question,
                        Oneline = oneline,
                        IsSelected = false
                    });
                }
            }

            CoreCandidates = new ObservableCollection<CoreCandidate>(candidates);
            UseCustomCore = false;
        }

        private string ExtractValue(string[] lines, string key)
        {
            var line = lines.FirstOrDefault(l => l.TrimStart().StartsWith(key + ":"));
            if (line == null) return string.Empty;

            var colonIndex = line.IndexOf(':');
            if (colonIndex < 0) return string.Empty;

            return line.Substring(colonIndex + 1).Trim();
        }

        private bool CanAdoptCore()
        {
            if (UseCustomCore)
            {
                return !string.IsNullOrWhiteSpace(CustomCoreSubject) &&
                       !string.IsNullOrWhiteSpace(CustomCoreEmotion) &&
                       !string.IsNullOrWhiteSpace(CustomCoreQuestion) &&
                       !string.IsNullOrWhiteSpace(CustomCoreOneline);
            }
            return CoreCandidates.Any(c => c.IsSelected);
        }

        private void AdoptCore()
        {
            if (CurrentStep == null) return;

            string bodyText;
            string source;

            if (UseCustomCore)
            {
                bodyText = $"主題: {CustomCoreSubject}\n" +
                           $"中心感情: {CustomCoreEmotion}\n" +
                           $"問い/変化: {CustomCoreQuestion}\n" +
                           $"核の一文: {CustomCoreOneline}\n" +
                           $"Source: CUSTOM";
                source = "CUSTOM";
            }
            else
            {
                var selected = CoreCandidates.FirstOrDefault(c => c.IsSelected);
                if (selected == null)
                {
                    StatusMessage = "候補を選択してください";
                    return;
                }

                bodyText = $"主題: {selected.Subject}\n" +
                           $"中心感情: {selected.Emotion}\n" +
                           $"問い/変化: {selected.Question}\n" +
                           $"核の一文: {selected.Oneline}\n" +
                           $"Source: CANDIDATE_{selected.Index}";
                source = $"CANDIDATE_{selected.Index}";
            }

            _db.CreateTextAsset(_projectId, _runId, null, "CORE", "[\"CORE_CANDIDATES\"]", bodyText);

            CurrentStep.Status = "SUCCESS";
            StatusMessage = $"Core採択完了 (Source: {source})";
            MoveToNextStep();
        }

        #endregion

        #region Navigation

        private void MoveToNextStep()
        {
            if (CurrentStep == null) return;

            var currentIndex = Steps.IndexOf(CurrentStep);
            if (currentIndex < Steps.Count - 1)
            {
                CurrentStep = Steps[currentIndex + 1];
                PrepareStep(CurrentStep);

                // 自動モードかつAIステップなら自動継続
                if (IsAutoMode && !CurrentStep.IsHumanStep && CurrentStep.Name != "EXPORT")
                {
                    _ = ExecuteAutoModeAsync();
                }
            }
            else
            {
                _runService.Complete(_runId);
                StatusMessage = "Run完了！";
            }
        }

        private void ProceedToCompare()
        {
            _mainViewModel.CurrentView = new PoetryLabCompareViewModel(_mainViewModel, _db, _projectId, _runId);
        }

        private void GoBack()
        {
            _mainViewModel.CurrentView = new PoetryLabProjectViewModel(_mainViewModel, _db, _projectId);
        }

        private void CancelRun()
        {
            var result = MessageBox.Show(
                "Runをキャンセルしますか？\n進行中のデータは保存されます。",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _runService.Cancel(_runId);
                GoBack();
            }
        }

        #endregion

        #region Utility

        private void RetryStep()
        {
            if (CurrentStep == null) return;

            CurrentStep.Status = "PENDING";
            IsManualMode = false;
            ManualInput = string.Empty;
            PrepareStep(CurrentStep);
        }

        private void CopyPrompt()
        {
            if (!string.IsNullOrEmpty(CurrentPrompt))
            {
                try
                {
                    Clipboard.SetText(CurrentPrompt);
                    StatusMessage = "プロンプトをクリップボードにコピーしました";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"コピー失敗: {ex.Message}";
                }
            }
        }

        private void ViewPreviousResult()
        {
            if (CurrentStep == null || CurrentStep.Index <= 1) return;
            LoadPreviousStepResult(CurrentStep);
        }

        #endregion
    }
}
