using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    /// <summary>
    /// MindsetLab ホーム画面 ViewModel
    /// </summary>
    public class MindsetLabHomeViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly MindsetDatabaseService _db;

        private ObservableCollection<MsDayListItem> _recentDays = new();
        public ObservableCollection<MsDayListItem> RecentDays
        {
            get => _recentDays;
            set { _recentDays = value; OnPropertyChanged(); }
        }

        private int _consecutiveDays;
        public int ConsecutiveDays
        {
            get => _consecutiveDays;
            set { _consecutiveDays = value; OnPropertyChanged(); }
        }

        private string _todayStatus = string.Empty;
        public string TodayStatus
        {
            get => _todayStatus;
            set { _todayStatus = value; OnPropertyChanged(); }
        }

        private bool _hasTodaySession;
        public bool HasTodaySession
        {
            get => _hasTodaySession;
            set { _hasTodaySession = value; OnPropertyChanged(); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // コマンド
        public ICommand StartTodayCommand { get; }
        public ICommand ContinueTodayCommand { get; }
        public ICommand OpenDayCommand { get; }
        public ICommand ViewHistoryCommand { get; }
        public ICommand RefreshCommand { get; }

        public MindsetLabHomeViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _db = new MindsetDatabaseService();

            // DB初期化
            MindsetDatabaseService.InitializeDatabase();

            StartTodayCommand = new RelayCommand(StartToday);
            ContinueTodayCommand = new RelayCommand(ContinueToday);
            OpenDayCommand = new RelayCommand<MsDayListItem>(OpenDay);
            ViewHistoryCommand = new RelayCommand(ViewHistory);
            RefreshCommand = new RelayCommand(LoadData);

            LoadData();
        }

        private void LoadData()
        {
            // 継続日数
            ConsecutiveDays = _db.GetConsecutiveDays();

            // 今日のセッション確認
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var todayDay = _db.GetDayByDate(today);
            HasTodaySession = todayDay != null;

            if (HasTodaySession && todayDay != null)
            {
                var review = _db.GetReviewByDay(todayDay.Id);
                if (review != null)
                {
                    TodayStatus = $"✅ 完了（{review.TotalScore}点）";
                }
                else
                {
                    var entries = _db.GetEntriesByDay(todayDay.Id);
                    TodayStatus = entries.Count > 0 ? "📝 入力中..." : "🚀 開始済み";
                }
            }
            else
            {
                TodayStatus = "まだ開始していません";
            }

            // 最近の記録
            var days = _db.GetRecentDays(10);
            var items = days.Select(d =>
            {
                var review = _db.GetReviewByDay(d.Id);
                var mindsetNames = d.GetFocusMindsetList()
                    .Select(id => MindsetDefinitions.GetMindsetShortName(id))
                    .ToList();

                return new MsDayListItem
                {
                    DayId = d.Id,
                    DateKey = d.DateKey,
                    FocusMindsets = string.Join(", ", mindsetNames),
                    TotalScore = review?.TotalScore,
                    IsToday = d.DateKey == today
                };
            }).ToList();

            RecentDays = new ObservableCollection<MsDayListItem>(items);
            StatusMessage = $"継続 {ConsecutiveDays} 日";
        }

        private void StartToday()
        {
            var dayId = _db.GetOrCreateToday();
            _mainViewModel.CurrentView = new MindsetLabSessionViewModel(_mainViewModel, _db, dayId);
        }

        private void ContinueToday()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var todayDay = _db.GetDayByDate(today);
            if (todayDay != null)
            {
                _mainViewModel.CurrentView = new MindsetLabSessionViewModel(_mainViewModel, _db, todayDay.Id);
            }
            else
            {
                StartToday();
            }
        }

        private void OpenDay(MsDayListItem? item)
        {
            if (item == null) return;
            _mainViewModel.CurrentView = new MindsetLabSessionViewModel(_mainViewModel, _db, item.DayId);
        }

        private void ViewHistory()
        {
            _mainViewModel.CurrentView = new MindsetLabHistoryViewModel(_mainViewModel, _db);
        }
    }

    /// <summary>
    /// 日別リスト表示用アイテム
    /// </summary>
    public class MsDayListItem
    {
        public int DayId { get; set; }
        public string DateKey { get; set; } = string.Empty;
        public string FocusMindsets { get; set; } = string.Empty;
        public int? TotalScore { get; set; }
        public bool IsToday { get; set; }

        public string ScoreDisplay => TotalScore.HasValue ? $"{TotalScore}点" : "-";
        public string DateDisplay => IsToday ? $"{DateKey} (今日)" : DateKey;
    }
}
