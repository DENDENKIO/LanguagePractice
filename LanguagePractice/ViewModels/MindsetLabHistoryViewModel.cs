using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    /// <summary>
    /// MindsetLab 履歴表示 ViewModel
    /// </summary>
    public class MindsetLabHistoryViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly MindsetDatabaseService _db;
        private readonly MindsetExportService _exportService;

        private ObservableCollection<MsDayHistoryItem> _days = new();
        public ObservableCollection<MsDayHistoryItem> Days
        {
            get => _days;
            set { _days = value; OnPropertyChanged(); }
        }

        private MsDayHistoryItem? _selectedDay;
        public MsDayHistoryItem? SelectedDay
        {
            get => _selectedDay;
            set
            {
                _selectedDay = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedDay));
                if (value != null)
                {
                    LoadDayDetail(value.DayId);
                }
            }
        }

        public bool HasSelectedDay => SelectedDay != null;

        private string _detailText = string.Empty;
        public string DetailText
        {
            get => _detailText;
            set { _detailText = value; OnPropertyChanged(); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // コマンド
        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand OpenDayCommand { get; }

        public MindsetLabHistoryViewModel(MainViewModel mainViewModel, MindsetDatabaseService db)
        {
            _mainViewModel = mainViewModel;
            _db = db;
            _exportService = new MindsetExportService(db);

            BackCommand = new RelayCommand(GoBack);
            RefreshCommand = new RelayCommand(LoadData);
            ExportCommand = new RelayCommand(ExportSelected, () => SelectedDay != null);
            OpenDayCommand = new RelayCommand<int>(OpenDay);

            LoadData();
        }

        // dayId を受け取るオーバーロード（互換性のため）
        public MindsetLabHistoryViewModel(MainViewModel mainViewModel, MindsetDatabaseService db, int dayId)
            : this(mainViewModel, db)
        {
            // dayId がある場合はその日を選択
            var targetDay = Days.FirstOrDefault(d => d.DayId == dayId);
            if (targetDay != null)
            {
                SelectedDay = targetDay;
            }
        }

        private void LoadData()
        {
            var days = _db.GetAllDays();
            var items = days.Select(d =>
            {
                var review = _db.GetReviewByDay(d.Id);
                return new MsDayHistoryItem
                {
                    DayId = d.Id,
                    DateKey = d.DateKey,
                    FocusMindsets = d.FocusMindsets,
                    Scene = d.Scene,
                    TotalScore = review?.TotalScore,
                    HasReview = review != null
                };
            }).ToList();

            Days = new ObservableCollection<MsDayHistoryItem>(items);
            StatusMessage = $"{Days.Count}件の記録";
        }

        private void LoadDayDetail(int dayId)
        {
            var day = _db.GetDay(dayId);
            if (day == null)
            {
                DetailText = "(データなし)";
                return;
            }

            var entries = _db.GetEntriesByDay(dayId);
            var review = _db.GetReviewByDay(dayId);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"【日付】{day.DateKey}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(day.FocusMindsets))
            {
                var mindsetNames = day.GetFocusMindsetList()
                    .Select(id => $"{id}. {MindsetDefinitions.GetMindsetName(id)}")
                    .ToList();
                sb.AppendLine($"【重点マインドセット】");
                sb.AppendLine(string.Join("\n", mindsetNames));
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(day.Scene))
            {
                sb.AppendLine($"【シーン】");
                sb.AppendLine(day.Scene);
                sb.AppendLine();
            }

            if (entries.Count > 0)
            {
                sb.AppendLine("【入力記録】");
                foreach (var entry in entries.OrderBy(e => e.EntryType))
                {
                    var drillTitle = GetDrillTitle(entry.EntryType);
                    sb.AppendLine($"[{entry.EntryType}] {drillTitle}");
                    sb.AppendLine(entry.BodyText);
                    sb.AppendLine();
                }
            }

            if (review != null)
            {
                sb.AppendLine("【AIレビュー】");
                sb.AppendLine($"総合スコア: {review.TotalScore}点");

                if (!string.IsNullOrEmpty(review.Strengths))
                {
                    sb.AppendLine($"強み: {review.Strengths}");
                }
                if (!string.IsNullOrEmpty(review.Weaknesses))
                {
                    sb.AppendLine($"改善点: {review.Weaknesses}");
                }
                if (!string.IsNullOrEmpty(review.NextDayPlan))
                {
                    sb.AppendLine($"明日の課題: {review.NextDayPlan}");
                }
                if (!string.IsNullOrEmpty(review.CoreLink))
                {
                    sb.AppendLine($"核候補: {review.CoreLink}");
                }
            }

            DetailText = sb.ToString();
        }

        private string GetDrillTitle(string entryType)
        {
            foreach (var m in MindsetDefinitions.All.Values)
            {
                foreach (var d in m.Drills)
                {
                    if (d.EntryType == entryType) return d.Title;
                }
            }
            return entryType;
        }

        private void ExportSelected()
        {
            if (SelectedDay == null) return;

            try
            {
                var path = _exportService.ExportDay(SelectedDay.DayId);
                StatusMessage = $"エクスポート完了: {path}";
                MessageBox.Show($"エクスポートしました:\n{path}", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"エクスポート失敗: {ex.Message}";
                MessageBox.Show($"エクスポートに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenDay(int dayId)
        {
            _mainViewModel.CurrentView = new MindsetLabSessionViewModel(_mainViewModel, _db, dayId);
        }

        private void GoBack()
        {
            _mainViewModel.CurrentView = new MindsetLabHomeViewModel(_mainViewModel);
        }
    }

    /// <summary>
    /// 履歴表示用アイテム
    /// </summary>
    public class MsDayHistoryItem
    {
        public int DayId { get; set; }
        public string DateKey { get; set; } = string.Empty;
        public string FocusMindsets { get; set; } = string.Empty;
        public string Scene { get; set; } = string.Empty;
        public int? TotalScore { get; set; }
        public bool HasReview { get; set; }

        public string ScoreDisplay => TotalScore.HasValue ? $"{TotalScore}点" : "-";
        public string FocusDisplay => string.IsNullOrEmpty(FocusMindsets) ? "-" : FocusMindsets;
    }
}
