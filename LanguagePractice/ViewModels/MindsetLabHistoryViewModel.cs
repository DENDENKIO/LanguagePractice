using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    /// <summary>
    /// MindsetLab 履歴詳細画面 ViewModel
    /// 仕様書2 第4.4章 / 第7.1章準拠
    /// </summary>
    public class MindsetLabHistoryViewModel : ViewModelBase
    {
        private readonly MindsetDatabaseService _db;
        private readonly MindsetExportService _exportService;
        private readonly MainViewModel _mainViewModel;
        private readonly int _dayId;

        private MsDay _day = new();
        public MsDay Day
        {
            get => _day;
            set { _day = value; OnPropertyChanged(); }
        }

        private List<MsEntry> _entries = new();
        public List<MsEntry> Entries
        {
            get => _entries;
            set { _entries = value; OnPropertyChanged(); }
        }

        private MsReview? _review;
        public MsReview? Review
        {
            get => _review;
            set { _review = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasReview)); }
        }

        public bool HasReview => Review != null;

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // 表示用のグループ化されたエントリ
        private List<MindsetEntryGroup> _groupedEntries = new();
        public List<MindsetEntryGroup> GroupedEntries
        {
            get => _groupedEntries;
            set { _groupedEntries = value; OnPropertyChanged(); }
        }

        // コマンド
        public ICommand ExportCommand { get; }
        public ICommand BackCommand { get; }

        public MindsetLabHistoryViewModel(MainViewModel mainViewModel, MindsetDatabaseService db, int dayId)
        {
            _mainViewModel = mainViewModel;
            _db = db;
            _dayId = dayId;
            _exportService = new MindsetExportService(db);

            ExportCommand = new RelayCommand(Export);
            BackCommand = new RelayCommand(GoBack);

            LoadData();
        }

        private void LoadData()
        {
            Day = _db.GetDay(_dayId) ?? new MsDay();
            Entries = _db.GetEntriesByDay(_dayId);
            Review = _db.GetReviewByDay(_dayId);

            // グループ化
            var groups = new List<MindsetEntryGroup>();
            var groupedDict = new Dictionary<int, List<MsEntry>>();

            foreach (var entry in Entries)
            {
                int mindsetId = GetMindsetIdFromEntryType(entry.EntryType);
                if (!groupedDict.ContainsKey(mindsetId))
                    groupedDict[mindsetId] = new List<MsEntry>();
                groupedDict[mindsetId].Add(entry);
            }

            foreach (var kvp in groupedDict.OrderBy(x => x.Key))
            {
                groups.Add(new MindsetEntryGroup
                {
                    MindsetId = kvp.Key,
                    MindsetName = MindsetDefinitions.GetMindsetName(kvp.Key),
                    Entries = kvp.Value
                });
            }

            GroupedEntries = groups;

            StatusMessage = $"履歴: {Day.DateKey}";
        }

        private int GetMindsetIdFromEntryType(string entryType)
        {
            if (entryType.StartsWith("A")) return 1;
            if (entryType.StartsWith("B")) return 2;
            if (entryType.StartsWith("C")) return 3;
            if (entryType.StartsWith("D")) return 4;
            if (entryType.StartsWith("E")) return 5;
            if (entryType.StartsWith("F")) return 6;
            return 0;
        }

        private void Export()
        {
            try
            {
                var filePath = _exportService.ExportDay(_dayId);
                StatusMessage = $"エクスポート完了: {filePath}";

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

        private void GoBack()
        {
            _mainViewModel.CurrentView = new MindsetLabHomeViewModel(_mainViewModel);
        }
    }

    /// <summary>
    /// マインドセット別エントリグループ（表示用）
    /// </summary>
    public class MindsetEntryGroup
    {
        public int MindsetId { get; set; }
        public string MindsetName { get; set; } = string.Empty;
        public List<MsEntry> Entries { get; set; } = new();

        public string DisplayHeader => $"Mindset {MindsetId}: {MindsetName}";
    }
}
