using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    /// <summary>
    /// PoetryLab プロジェクト詳細画面 ViewModel
    /// </summary>
    public class PoetryLabProjectViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly PoetryDatabaseService _db;
        private readonly PoetryRunService _runService;
        private readonly PoetryExportService _exportService;
        private readonly int _projectId;

        private PlProject? _project;
        public PlProject? Project
        {
            get => _project;
            set { _project = value; OnPropertyChanged(); }
        }

        private ObservableCollection<PlRun> _runs = new();
        public ObservableCollection<PlRun> Runs
        {
            get => _runs;
            set { _runs = value; OnPropertyChanged(); }
        }

        private PlRun? _selectedRun;
        public PlRun? SelectedRun
        {
            get => _selectedRun;
            set
            {
                _selectedRun = value;
                OnPropertyChanged();
                LoadRunAssets();
            }
        }

        private ObservableCollection<PlTextAsset> _assets = new();
        public ObservableCollection<PlTextAsset> Assets
        {
            get => _assets;
            set { _assets = value; OnPropertyChanged(); }
        }

        private PlTextAsset? _selectedAsset;
        public PlTextAsset? SelectedAsset
        {
            get => _selectedAsset;
            set { _selectedAsset = value; OnPropertyChanged(); }
        }

        private string _previewText = string.Empty;
        public string PreviewText
        {
            get => _previewText;
            set { _previewText = value; OnPropertyChanged(); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // コマンド
        public ICommand BackCommand { get; }
        public ICommand StartRunCommand { get; }
        public ICommand ContinueRunCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ViewIssuesCommand { get; }
        public ICommand ViewCompareHistoryCommand { get; }
        public ICommand SelectAssetCommand { get; }

        public PoetryLabProjectViewModel(MainViewModel mainViewModel, PoetryDatabaseService db, int projectId)
        {
            _mainViewModel = mainViewModel;
            _db = db;
            _projectId = projectId;
            _runService = new PoetryRunService(_db);
            _exportService = new PoetryExportService(_db);

            BackCommand = new RelayCommand(GoBack);
            StartRunCommand = new RelayCommand(StartNewRun);
            ContinueRunCommand = new RelayCommand(ContinueRun, CanContinueRun);
            ExportCommand = new RelayCommand(ExportRun, CanExportRun);
            ViewIssuesCommand = new RelayCommand(ViewIssues);
            ViewCompareHistoryCommand = new RelayCommand(ViewCompareHistory);
            SelectAssetCommand = new RelayCommand<PlTextAsset>(SelectAsset);

            LoadProject();
            LoadRuns();
        }

        private void LoadProject()
        {
            Project = _db.GetProject(_projectId);
        }

        private void LoadRuns()
        {
            var runs = _runService.GetByProject(_projectId);
            Runs = new ObservableCollection<PlRun>(runs);

            // 最新のRunを選択
            if (Runs.Count > 0)
            {
                SelectedRun = Runs[0];
            }
        }

        private void LoadRunAssets()
        {
            if (SelectedRun == null)
            {
                Assets = new ObservableCollection<PlTextAsset>();
                PreviewText = string.Empty;
                return;
            }

            var assets = _runService.GetAssets(SelectedRun.Id);
            Assets = new ObservableCollection<PlTextAsset>(assets);

            // 最初の成果物をプレビュー
            if (Assets.Count > 0)
            {
                SelectAsset(Assets[0]);
            }
            else
            {
                PreviewText = "(成果物なし)";
            }
        }

        private void SelectAsset(PlTextAsset? asset)
        {
            if (asset == null) return;
            SelectedAsset = asset;
            PreviewText = asset.BodyText;
        }

        private void GoBack()
        {
            _mainViewModel.CurrentView = new PoetryLabHomeViewModel(_mainViewModel);
        }

        private void StartNewRun()
        {
            if (Project == null) return;

            var run = _runService.Create(_projectId);
            _mainViewModel.CurrentView = new PoetryLabRunViewModel(_mainViewModel, _db, _projectId, run.Id);
        }

        private bool CanContinueRun()
        {
            return SelectedRun != null && SelectedRun.Status == "RUNNING";
        }

        private void ContinueRun()
        {
            if (SelectedRun == null) return;
            _mainViewModel.CurrentView = new PoetryLabRunViewModel(_mainViewModel, _db, _projectId, SelectedRun.Id);
        }

        private bool CanExportRun()
        {
            return SelectedRun != null && SelectedRun.Status == "SUCCESS";
        }

        private void ExportRun()
        {
            if (SelectedRun == null) return;

            var result = _exportService.Export(_projectId, SelectedRun.Id);
            if (result.Success)
            {
                StatusMessage = $"エクスポート完了: {result.FilePath}";
                // View側でダイアログ表示を想定
            }
            else
            {
                StatusMessage = $"エクスポート失敗: {result.ErrorMessage}";
            }
        }

        private void ViewIssues()
        {
            if (SelectedRun == null) return;
            // Issue一覧表示（モーダルまたは別View）
            // 今回はシンプルにプレビューエリアに表示
            var issues = _runService.GetIssues(SelectedRun.Id);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Issues ===");
            foreach (var issue in issues)
            {
                sb.AppendLine($"[{issue.Severity}] {issue.Level} - {issue.Symptom}");
            }
            PreviewText = sb.ToString();
        }

        private void ViewCompareHistory()
        {
            if (SelectedRun == null) return;
            var compare = _runService.GetCompare(SelectedRun.Id);
            if (compare != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== Compare Result ===");
                sb.AppendLine($"Winner Asset ID: {compare.WinnerAssetId}");
                sb.AppendLine($"Reason: {compare.ReasonNote}");
                PreviewText = sb.ToString();
            }
            else
            {
                PreviewText = "(比較結果なし)";
            }
        }

        /// <summary>
        /// Run状態の表示文字列
        /// </summary>
        public string GetRunStatusDisplay(PlRun run)
        {
            return run.Status switch
            {
                "RUNNING" => "実行中",
                "SUCCESS" => "完了 ✓",
                "FAILED" => "失敗 ✗",
                "CANCELLED" => "中断",
                _ => run.Status
            };
        }
    }
}
