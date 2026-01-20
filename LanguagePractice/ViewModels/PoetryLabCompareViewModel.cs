using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    /// <summary>
    /// 改稿案の表示用モデル
    /// </summary>
    public class RevisionCandidate : ViewModelBase
    {
        public string Key { get; set; } = string.Empty; // REV_A, REV_B, REV_C
        public string Label { get; set; } = string.Empty; // "REVISION A"
        public string Approach { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int AssetId { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
    }

    /// <summary>
    /// PoetryLab 比較・Winner選択画面 ViewModel
    /// </summary>
    public class PoetryLabCompareViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly PoetryDatabaseService _db;
        private readonly PoetryRunService _runService;
        private readonly int _projectId;
        private readonly int _runId;

        private PlProject? _project;
        public PlProject? Project
        {
            get => _project;
            set { _project = value; OnPropertyChanged(); }
        }

        private string _coreText = string.Empty;
        public string CoreText
        {
            get => _coreText;
            set { _coreText = value; OnPropertyChanged(); }
        }

        private ObservableCollection<RevisionCandidate> _candidates = new();
        public ObservableCollection<RevisionCandidate> Candidates
        {
            get => _candidates;
            set { _candidates = value; OnPropertyChanged(); }
        }

        private RevisionCandidate? _selectedCandidate;
        public RevisionCandidate? SelectedCandidate
        {
            get => _selectedCandidate;
            set
            {
                // 排他選択
                foreach (var c in Candidates)
                {
                    c.IsSelected = false;
                }
                _selectedCandidate = value;
                if (_selectedCandidate != null)
                {
                    _selectedCandidate.IsSelected = true;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConfirm));
            }
        }

        private string _reasonNote = string.Empty;
        public string ReasonNote
        {
            get => _reasonNote;
            set { _reasonNote = value; OnPropertyChanged(); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool CanConfirm => SelectedCandidate != null;

        // 比較観点（参考）
        public string[] ComparisonPoints => new[]
        {
            "• 核の「問い」が結末でどう扱われているか",
            "• 像の一貫性（支柱の像の扱い）",
            "• 余韻の開き具合（閉じすぎ/開きすぎ）",
            "• 声の距離感の変化",
            "• 転回の効果"
        };

        // コマンド
        public ICommand SelectCandidateCommand { get; }
        public ICommand ConfirmWinnerCommand { get; }
        public ICommand BackCommand { get; }

        public PoetryLabCompareViewModel(MainViewModel mainViewModel, PoetryDatabaseService db, int projectId, int runId)
        {
            _mainViewModel = mainViewModel;
            _db = db;
            _projectId = projectId;
            _runId = runId;
            _runService = new PoetryRunService(_db);

            SelectCandidateCommand = new RelayCommand<RevisionCandidate>(SelectCandidate);
            ConfirmWinnerCommand = new RelayCommand(ConfirmWinner, () => CanConfirm);
            BackCommand = new RelayCommand(GoBack);

            LoadData();
        }

        private void LoadData()
        {
            Project = _db.GetProject(_projectId);

            // Core取得
            var coreAsset = _db.GetTextAssetByType(_runId, "CORE");
            CoreText = coreAsset?.BodyText ?? "(核が未確定)";

            // Revision取得
            var candidates = new List<RevisionCandidate>();
            var keys = new[] { ("REV_A", "REVISION A"), ("REV_B", "REVISION B"), ("REV_C", "REVISION C") };

            foreach (var (key, label) in keys)
            {
                var asset = _db.GetTextAssetByType(_runId, key);
                if (asset != null)
                {
                    // Approach抽出
                    var lines = asset.BodyText.Split('\n');
                    var approachLine = lines.FirstOrDefault(l => l.StartsWith("[Approach:"));
                    var approach = approachLine?.Replace("[Approach:", "").Replace("]", "").Trim() ?? "";
                    var body = string.Join("\n", lines.Skip(approachLine != null ? 2 : 0));

                    candidates.Add(new RevisionCandidate
                    {
                        Key = key,
                        Label = label,
                        Approach = approach,
                        Body = body,
                        AssetId = asset.Id
                    });
                }
            }

            Candidates = new ObservableCollection<RevisionCandidate>(candidates);
        }

        private void SelectCandidate(RevisionCandidate? candidate)
        {
            SelectedCandidate = candidate;
        }

        private void ConfirmWinner()
        {
            if (SelectedCandidate == null) return;

            // pl_compare 保存
            var candidateIds = JsonSerializer.Serialize(Candidates.Select(c => c.AssetId).ToList());
            _db.CreateCompare(_projectId, _runId, candidateIds, SelectedCandidate.AssetId, ReasonNote);

            // WINNER として保存
            var inputKeysJson = JsonSerializer.Serialize(new[] { SelectedCandidate.Key });
            _db.CreateTextAsset(_projectId, _runId, null, "WINNER", inputKeysJson, SelectedCandidate.Body);

            StatusMessage = "Winner確定";

            // Run画面に戻ってExportへ
            _mainViewModel.CurrentView = new PoetryLabRunViewModel(_mainViewModel, _db, _projectId, _runId);
        }

        private void GoBack()
        {
            _mainViewModel.CurrentView = new PoetryLabRunViewModel(_mainViewModel, _db, _projectId, _runId);
        }
    }
}
