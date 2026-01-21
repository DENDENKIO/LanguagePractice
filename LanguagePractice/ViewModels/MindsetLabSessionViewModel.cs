using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using LanguagePractice.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    /// <summary>
    /// MindsetLab セッション（入力フォーム）ViewModel
    /// </summary>
    public class MindsetLabSessionViewModel : ViewModelBase
    {
        private readonly MindsetDatabaseService _db;
        private readonly MindsetPromptBuilder _promptBuilder;
        private readonly MindsetOutputParser _parser;
        private readonly SettingsService _settingsService;
        private readonly MainViewModel _mainViewModel;
        private readonly int _dayId;

        private MsDay _day = new();
        public MsDay Day
        {
            get => _day;
            set { _day = value; OnPropertyChanged(); }
        }

        private ObservableCollection<DrillInputItem> _drillInputs = new();
        public ObservableCollection<DrillInputItem> DrillInputs
        {
            get => _drillInputs;
            set { _drillInputs = value; OnPropertyChanged(); }
        }

        private string _planPrompt = string.Empty;
        public string PlanPrompt
        {
            get => _planPrompt;
            set { _planPrompt = value; OnPropertyChanged(); }
        }

        private string _planOutput = string.Empty;
        public string PlanOutput
        {
            get => _planOutput;
            set { _planOutput = value; OnPropertyChanged(); }
        }

        private bool _isPlanGenerated;
        public bool IsPlanGenerated
        {
            get => _isPlanGenerated;
            set { _isPlanGenerated = value; OnPropertyChanged(); }
        }

        private bool _isGenerating;
        public bool IsGenerating
        {
            get => _isGenerating;
            set { _isGenerating = value; OnPropertyChanged(); }
        }

        private bool _isManualMode;
        public bool IsManualMode
        {
            get => _isManualMode;
            set { _isManualMode = value; OnPropertyChanged(); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // ミッション表示用
        private ObservableCollection<string> _todayTasks = new();
        public ObservableCollection<string> TodayTasks
        {
            get => _todayTasks;
            set { _todayTasks = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasTasks)); }
        }
        public bool HasTasks => TodayTasks != null && TodayTasks.Count > 0;

        private string _scene = string.Empty;
        public string Scene
        {
            get => _scene;
            set { _scene = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasScene)); }
        }
        public bool HasScene => !string.IsNullOrWhiteSpace(Scene);

        private string _focusMindsetDisplay = string.Empty;
        public string FocusMindsetDisplay
        {
            get => _focusMindsetDisplay;
            set { _focusMindsetDisplay = value; OnPropertyChanged(); }
        }

        // 互換性のため残す（非表示）
        private string _startRitual = string.Empty;
        public string StartRitual
        {
            get => _startRitual;
            set { _startRitual = value; OnPropertyChanged(); }
        }

        private string _endRitual = string.Empty;
        public string EndRitual
        {
            get => _endRitual;
            set { _endRitual = value; OnPropertyChanged(); }
        }

        // コマンド
        public ICommand GeneratePlanAutoCommand { get; }
        public ICommand ApplyPlanCommand { get; }
        public ICommand SaveAllCommand { get; }
        public ICommand GoToReviewCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand CopyPromptCommand { get; }
        public ICommand SwitchToManualCommand { get; }
        public ICommand ViewHistoryCommand { get; }

        public MindsetLabSessionViewModel(MainViewModel mainViewModel, MindsetDatabaseService db, int dayId)
        {
            _mainViewModel = mainViewModel;
            _db = db;
            _dayId = dayId;
            _promptBuilder = new MindsetPromptBuilder();
            _parser = new MindsetOutputParser();
            _settingsService = new SettingsService();

            GeneratePlanAutoCommand = new RelayCommand(GeneratePlanAuto, () => !IsGenerating);
            ApplyPlanCommand = new RelayCommand(ApplyPlanManual, () => !string.IsNullOrEmpty(PlanOutput));
            SaveAllCommand = new RelayCommand(SaveAll);
            GoToReviewCommand = new RelayCommand(GoToReview);
            BackCommand = new RelayCommand(GoBack);
            CopyPromptCommand = new RelayCommand(CopyPrompt);
            SwitchToManualCommand = new RelayCommand(SwitchToManual);
            ViewHistoryCommand = new RelayCommand(ViewHistory);

            LoadData();
        }

        private void LoadData()
        {
            Day = _db.GetDay(_dayId) ?? new MsDay();

            // 既存の入力を読み込み
            var entries = _db.GetEntriesByDay(_dayId);

            // AIプラン確認
            var planLog = _db.GetLatestAiStepLog(_dayId, "MS_PLAN_GEN");
            if (planLog != null && planLog.Status == "DONE" && !string.IsNullOrEmpty(planLog.RawOutput))
            {
                IsPlanGenerated = true;
                var result = _parser.ParsePlanGen(planLog.RawOutput);
                if (result != null)
                {
                    ApplyPlanResult(result);
                }
            }

            // DBからシーンを復元
            if (!string.IsNullOrEmpty(Day.Scene))
            {
                Scene = Day.Scene;
            }

            // ドリル入力項目を生成
            LoadDrillInputs(entries);

            StatusMessage = $"日付: {Day.DateKey}";
        }

        private void LoadDrillInputs(List<MsEntry> entries)
        {
            var items = new List<DrillInputItem>();
            var focusMindsets = Day.GetFocusMindsetList();

            // 重点マインドセットがあればそれを優先、なければ全部
            var mindsetsToShow = focusMindsets.Count > 0
                ? focusMindsets
                : MindsetDefinitions.All.Keys.ToList();

            foreach (var mindsetId in mindsetsToShow.OrderBy(x => x))
            {
                if (MindsetDefinitions.All.TryGetValue(mindsetId, out var mindset))
                {
                    foreach (var drill in mindset.Drills)
                    {
                        var existing = entries.FirstOrDefault(e => e.EntryType == drill.EntryType);
                        items.Add(new DrillInputItem
                        {
                            MindsetId = mindsetId,
                            MindsetName = mindset.Name,
                            EntryType = drill.EntryType,
                            Title = drill.Title,
                            Hint = drill.Hint,
                            BodyText = existing?.BodyText ?? string.Empty
                        });
                    }
                }
            }

            DrillInputs = new ObservableCollection<DrillInputItem>(items);
        }

        /// <summary>
        /// パース結果を適用
        /// </summary>
        private void ApplyPlanResult(MsPlanResult result)
        {
            // タスク
            TodayTasks = new ObservableCollection<string>(result.Tasks);

            // シーン
            Scene = result.Scene ?? string.Empty;

            // 儀式（互換性のため残すが非表示）
            StartRitual = result.StartRitual ?? string.Empty;
            EndRitual = result.EndRitual ?? string.Empty;

            // 重点マインドセット表示
            if (result.FocusMindsets.Count > 0)
            {
                var names = result.FocusMindsets
                    .Select(id => $"{id}. {MindsetDefinitions.GetMindsetName(id)}")
                    .ToList();
                FocusMindsetDisplay = string.Join("\n", names);
            }
            else
            {
                FocusMindsetDisplay = "(未設定)";
            }

            // デバッグ出力
            System.Diagnostics.Debug.WriteLine($"=== ApplyPlanResult ===");
            System.Diagnostics.Debug.WriteLine($"FocusMindsets: {string.Join(",", result.FocusMindsets)}");
            System.Diagnostics.Debug.WriteLine($"Scene: [{Scene}]");
            System.Diagnostics.Debug.WriteLine($"Tasks: {TodayTasks.Count}");
        }

        /// <summary>
        /// AI自動実行（BrowserWindow使用）
        /// </summary>
        private void GeneratePlanAuto()
        {
            if (IsGenerating) return;

            IsGenerating = true;
            IsManualMode = false;
            StatusMessage = "AIミッション生成を開始...";

            try
            {
                // 前回の弱点を取得
                string? previousWeakness = GetPreviousWeakness();
                var consecutiveDays = _db.GetConsecutiveDays();

                // プロンプト生成
                PlanPrompt = _promptBuilder.BuildPlanGenPrompt(consecutiveDays, previousWeakness);

                // AI設定取得
                string siteId = _settingsService.GetValue("AI_SITE_ID", "GENSPARK");
                var profile = AiSiteCatalog.GetByIdOrDefault(siteId);
                string aiUrl = _settingsService.GetValue("AI_URL", profile.Url);

                if (string.IsNullOrEmpty(aiUrl))
                {
                    aiUrl = profile.Url;
                }

                // プロンプトをクリップボードにコピー
                Clipboard.SetText(PlanPrompt);
                StatusMessage = "プロンプトをコピーしました。ブラウザを開きます...";

                // BrowserWindow（WebView2）を使用
                var browser = new BrowserWindow(aiUrl, PlanPrompt, profile.Id);

                if (browser.ShowDialog() == true)
                {
                    string result = browser.ResultText;

                    // デバッグ: 取得した結果を出力
                    System.Diagnostics.Debug.WriteLine($"=== BrowserWindow Result ===");
                    System.Diagnostics.Debug.WriteLine($"Length: {result?.Length ?? 0}");
                    if (result != null && result.Length > 500)
                    {
                        System.Diagnostics.Debug.WriteLine(result.Substring(0, 500) + "...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(result ?? "(null)");
                    }
                    System.Diagnostics.Debug.WriteLine($"=== END ===");

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        StatusMessage = "AI出力を取得しました。解析中...";
                        ProcessPlanOutput(result);
                    }
                    else
                    {
                        StatusMessage = "自動取得できませんでした。手動で貼り付けてください。";
                        IsManualMode = true;
                    }
                }
                else
                {
                    StatusMessage = "ブラウザ操作がキャンセルされました。手動モードで続行できます。";
                    IsManualMode = true;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                IsManualMode = true;
                System.Diagnostics.Debug.WriteLine($"GeneratePlanAuto error: {ex}");
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private string? GetPreviousWeakness()
        {
            var recentDays = _db.GetRecentDays(5);
            foreach (var day in recentDays)
            {
                if (day.Id == _dayId) continue;
                var review = _db.GetReviewByDay(day.Id);
                if (review != null && !string.IsNullOrEmpty(review.Weaknesses))
                {
                    return review.Weaknesses.Split('\n').FirstOrDefault();
                }
            }
            return null;
        }

        /// <summary>
        /// プラン出力を処理
        /// </summary>
        private void ProcessPlanOutput(string rawOutput)
        {
            var result = _parser.ParsePlanGen(rawOutput);
            if (result == null)
            {
                StatusMessage = "パースに失敗しました。手動で修正してください。";
                PlanOutput = rawOutput;
                IsManualMode = true;
                return;
            }

            // FocusMindsetsが空の場合は警告
            if (result.FocusMindsets.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("警告: FocusMindsetsが空です");
            }

            // DBに保存
            var logId = _db.CreateAiStepLog(_dayId, "MS_PLAN_GEN", PlanPrompt);
            _db.UpdateAiStepLogResult(logId, rawOutput, _parser.ToJson(result), "DONE");

            // Day更新
            var focusMindsets = string.Join(",", result.FocusMindsets);
            _db.UpdateDay(_dayId, focusMindsets, result.Scene, result.StartRitual, result.EndRitual);

            // 表示更新
            IsPlanGenerated = true;
            IsManualMode = false;
            ApplyPlanResult(result);

            // ドリル入力を再読み込み（重点マインドセットが変わった場合）
            Day = _db.GetDay(_dayId) ?? Day;
            var entries = _db.GetEntriesByDay(_dayId);
            LoadDrillInputs(entries);

            StatusMessage = "✅ 今日のミッションを生成・適用しました！";
        }

        private void ApplyPlanManual()
        {
            if (string.IsNullOrEmpty(PlanOutput))
            {
                StatusMessage = "AI出力を貼り付けてください。";
                return;
            }

            ProcessPlanOutput(PlanOutput);
        }

        private void SwitchToManual()
        {
            string? previousWeakness = GetPreviousWeakness();
            var consecutiveDays = _db.GetConsecutiveDays();
            PlanPrompt = _promptBuilder.BuildPlanGenPrompt(consecutiveDays, previousWeakness);

            IsManualMode = true;
            StatusMessage = "手動モードに切り替えました。プロンプトをコピーしてAIに貼り付けてください。";
        }

        private void CopyPrompt()
        {
            if (string.IsNullOrEmpty(PlanPrompt))
            {
                string? previousWeakness = GetPreviousWeakness();
                var consecutiveDays = _db.GetConsecutiveDays();
                PlanPrompt = _promptBuilder.BuildPlanGenPrompt(consecutiveDays, previousWeakness);
            }

            Clipboard.SetText(PlanPrompt);
            StatusMessage = "プロンプトをクリップボードにコピーしました。";
        }

        private void SaveAll()
        {
            int savedCount = 0;
            foreach (var item in DrillInputs)
            {
                if (!string.IsNullOrWhiteSpace(item.BodyText))
                {
                    _db.UpsertEntry(_dayId, item.EntryType, item.BodyText);
                    savedCount++;
                }
            }
            StatusMessage = $"💾 {savedCount}件を保存しました（{DateTime.Now:HH:mm:ss}）";
        }

        private void GoToReview()
        {
            SaveAll();
            _mainViewModel.CurrentView = new MindsetLabReviewViewModel(_mainViewModel, _db, _dayId);
        }

        private void GoBack()
        {
            _mainViewModel.CurrentView = new MindsetLabHomeViewModel(_mainViewModel);
        }

        private void ViewHistory()
        {
            _mainViewModel.CurrentView = new MindsetLabHistoryViewModel(_mainViewModel, _db, _dayId);
        }
    }

    /// <summary>
    /// ドリル入力項目（UI用）
    /// </summary>
    public class DrillInputItem : ViewModelBase
    {
        public int MindsetId { get; set; }
        public string MindsetName { get; set; } = string.Empty;
        public string EntryType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Hint { get; set; } = string.Empty;

        private string _bodyText = string.Empty;
        public string BodyText
        {
            get => _bodyText;
            set { _bodyText = value; OnPropertyChanged(); }
        }

        public string DisplayHeader => $"[{EntryType}] {Title}";
        public string DisplayHint => string.IsNullOrEmpty(Hint) ? "" : $"（{Hint}）";
    }
}
