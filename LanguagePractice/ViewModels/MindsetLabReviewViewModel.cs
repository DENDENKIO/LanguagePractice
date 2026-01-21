using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using LanguagePractice.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    /// <summary>
    /// MindsetLab レビュー画面 ViewModel
    /// 仕様書2 第4.3章 / 第7.2章準拠
    /// WebView2自動実行対応
    /// </summary>
    public class MindsetLabReviewViewModel : ViewModelBase
    {
        private readonly MindsetDatabaseService _db;
        private readonly MindsetPromptBuilder _promptBuilder;
        private readonly MindsetOutputParser _parser;
        private readonly MindsetExportService _exportService;
        private readonly SettingsService _settingsService;
        private readonly MainViewModel _mainViewModel;
        private readonly int _dayId;

        private MsDay _day = new();
        public MsDay Day
        {
            get => _day;
            set { _day = value; OnPropertyChanged(); }
        }

        private string _reviewPrompt = string.Empty;
        public string ReviewPrompt
        {
            get => _reviewPrompt;
            set { _reviewPrompt = value; OnPropertyChanged(); }
        }

        private string _reviewOutput = string.Empty;
        public string ReviewOutput
        {
            get => _reviewOutput;
            set { _reviewOutput = value; OnPropertyChanged(); }
        }

        private MsReview? _review;
        public MsReview? Review
        {
            get => _review;
            set { _review = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasReview)); }
        }

        public bool HasReview => Review != null;

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

        private int _entryCount;
        public int EntryCount
        {
            get => _entryCount;
            set { _entryCount = value; OnPropertyChanged(); }
        }

        // コマンド
        public ICommand GenerateReviewAutoCommand { get; }
        public ICommand ApplyReviewCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand BackToSessionCommand { get; }
        public ICommand BackToHomeCommand { get; }
        public ICommand CopyPromptCommand { get; }
        public ICommand SwitchToManualCommand { get; }

        public MindsetLabReviewViewModel(MainViewModel mainViewModel, MindsetDatabaseService db, int dayId)
        {
            _mainViewModel = mainViewModel;
            _db = db;
            _dayId = dayId;
            _promptBuilder = new MindsetPromptBuilder();
            _parser = new MindsetOutputParser();
            _exportService = new MindsetExportService(db);
            _settingsService = new SettingsService();

            GenerateReviewAutoCommand = new RelayCommand(GenerateReviewAuto, () => !IsGenerating && EntryCount > 0);
            ApplyReviewCommand = new RelayCommand(ApplyReviewManual, () => !string.IsNullOrEmpty(ReviewOutput));
            ExportCommand = new RelayCommand(Export, () => HasReview);
            BackToSessionCommand = new RelayCommand(BackToSession);
            BackToHomeCommand = new RelayCommand(BackToHome);
            CopyPromptCommand = new RelayCommand(CopyPrompt);
            SwitchToManualCommand = new RelayCommand(SwitchToManual);

            LoadData();
        }

        private void LoadData()
        {
            Day = _db.GetDay(_dayId) ?? new MsDay();
            Review = _db.GetReviewByDay(_dayId);

            var entries = _db.GetEntriesByDay(_dayId);
            EntryCount = entries.Count;

            StatusMessage = HasReview
                ? $"✅ レビュー完了（スコア: {Review!.TotalScore}点）"
                : $"入力済み: {EntryCount}件";
        }

        /// <summary>
        /// AI自動レビュー生成（BrowserWindow使用）
        /// </summary>
        private void GenerateReviewAuto()
        {
            if (IsGenerating) return;

            var entries = _db.GetEntriesByDay(_dayId);
            if (entries.Count == 0)
            {
                StatusMessage = "⚠️ 入力がありません。セッション画面で入力してください。";
                return;
            }

            IsGenerating = true;
            IsManualMode = false;
            StatusMessage = "AIレビュー生成を開始...";

            // プロンプト生成
            ReviewPrompt = _promptBuilder.BuildReviewPrompt(Day, entries);

            // AI設定取得
            string siteId = _settingsService.GetValue("AI_SITE_ID", "GENSPARK");
            var profile = AiSiteCatalog.GetByIdOrDefault(siteId);
            string aiUrl = _settingsService.GetValue("AI_URL", profile.Url);

            if (string.IsNullOrEmpty(aiUrl))
            {
                aiUrl = profile.Url;
            }

            // プロンプトをクリップボードにコピー
            Clipboard.SetText(ReviewPrompt);
            StatusMessage = "プロンプトをコピーしました。ブラウザを開きます...";

            // BrowserWindow（WebView2）を使用
            var browser = new BrowserWindow(aiUrl, ReviewPrompt, profile.Id);

            if (browser.ShowDialog() == true)
            {
                string result = browser.ResultText;

                if (!string.IsNullOrWhiteSpace(result))
                {
                    // 自動取得成功 → 解析して保存
                    StatusMessage = "AI出力を取得しました。解析中...";
                    ProcessReviewOutput(result);
                }
                else
                {
                    // 自動取得失敗 → 手動モードへ
                    StatusMessage = "自動取得できませんでした。手動で貼り付けてください。";
                    IsManualMode = true;
                }
            }
            else
            {
                StatusMessage = "ブラウザ操作がキャンセルされました。";
                IsManualMode = true;
            }

            IsGenerating = false;
        }

        /// <summary>
        /// レビュー出力を処理
        /// </summary>
        private void ProcessReviewOutput(string rawOutput)
        {
            var result = _parser.ParseReviewScore(rawOutput);
            if (result == null)
            {
                StatusMessage = "パースに失敗しました。手動で修正してください。";
                ReviewOutput = rawOutput;
                IsManualMode = true;
                return;
            }

            // DBに保存
            var logId = _db.CreateAiStepLog(_dayId, "MS_REVIEW_SCORE", ReviewPrompt);
            _db.UpdateAiStepLogResult(logId, rawOutput, _parser.ToJson(result), "DONE");

            // レビュー保存
            _db.CreateOrUpdateReview(
                _dayId,
                result.TotalScore,
                _parser.SubscoresToJson(result.Subscores),
                string.Join("\n", result.Strengths),
                string.Join("\n", result.Weaknesses),
                result.NextDayPlan,
                result.CoreLink
            );

            IsManualMode = false;
            LoadData();
            StatusMessage = $"✅ レビューを保存しました！（スコア: {result.TotalScore}点）";
        }

        /// <summary>
        /// 手動モードで貼り付けたレビューを適用
        /// </summary>
        private void ApplyReviewManual()
        {
            if (string.IsNullOrEmpty(ReviewOutput))
            {
                StatusMessage = "AI出力を貼り付けてください。";
                return;
            }

            ProcessReviewOutput(ReviewOutput);
        }

        /// <summary>
        /// 手動モードに切り替え
        /// </summary>
        private void SwitchToManual()
        {
            var entries = _db.GetEntriesByDay(_dayId);
            if (entries.Count == 0)
            {
                StatusMessage = "⚠️ 入力がありません。セッション画面で入力してください。";
                return;
            }

            ReviewPrompt = _promptBuilder.BuildReviewPrompt(Day, entries);
            IsManualMode = true;
            StatusMessage = "手動モードに切り替えました。プロンプトをコピーしてAIに投入し、結果を貼り付けてください。";
        }

        private void CopyPrompt()
        {
            var entries = _db.GetEntriesByDay(_dayId);
            if (entries.Count == 0)
            {
                StatusMessage = "⚠️ 入力がありません。";
                return;
            }

            ReviewPrompt = _promptBuilder.BuildReviewPrompt(Day, entries);
            Clipboard.SetText(ReviewPrompt);
            StatusMessage = "プロンプトをクリップボードにコピーしました。";
        }

        private void Export()
        {
            try
            {
                var filePath = _exportService.ExportDay(_dayId);
                StatusMessage = $"📁 エクスポート完了: {filePath}";

                // ファイルを開く
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"エクスポートエラー: {ex.Message}";
            }
        }

        private void BackToSession()
        {
            _mainViewModel.CurrentView = new MindsetLabSessionViewModel(_mainViewModel, _db, _dayId);
        }

        private void BackToHome()
        {
            _mainViewModel.CurrentView = new MindsetLabHomeViewModel(_mainViewModel);
        }
    }
}
