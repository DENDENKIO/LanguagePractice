using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    /// <summary>
    /// MindsetLab ホーム画面 ViewModel
    /// 仕様書2 第7.1章準拠
    /// </summary>
    public class MindsetLabHomeViewModel : ViewModelBase
    {
        private readonly MindsetDatabaseService _db;
        private readonly MainViewModel _mainViewModel;

        private ObservableCollection<MsDay> _recentDays = new();
        public ObservableCollection<MsDay> RecentDays
        {
            get => _recentDays;
            set { _recentDays = value; OnPropertyChanged(); }
        }

        private MsDay? _selectedDay;
        public MsDay? SelectedDay
        {
            get => _selectedDay;
            set { _selectedDay = value; OnPropertyChanged(); }
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

        // コマンド
        public ICommand StartTodayCommand { get; }
        public ICommand ContinueTodayCommand { get; }
        public ICommand OpenDayCommand { get; }
        public ICommand RefreshCommand { get; }

        public MindsetLabHomeViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _db = new MindsetDatabaseService();

            StartTodayCommand = new RelayCommand(StartToday);
            ContinueTodayCommand = new RelayCommand(ContinueToday);
            OpenDayCommand = new RelayCommand<MsDay>(OpenDay);
            RefreshCommand = new RelayCommand(LoadData);

            LoadData();
        }

        private void LoadData()
        {
            // 直近の履歴
            var days = _db.GetRecentDays(30);
            RecentDays = new ObservableCollection<MsDay>(days);

            // 継続日数
            ConsecutiveDays = _db.GetConsecutiveDays();

            // 今日のステータス
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var todayDay = _db.GetDayByDate(today);

            if (todayDay != null)
            {
                HasTodaySession = true;
                var entries = _db.GetEntriesByDay(todayDay.Id);
                var review = _db.GetReviewByDay(todayDay.Id);

                if (review != null)
                {
                    TodayStatus = $"✅ 完了（スコア: {review.TotalScore}点）";
                }
                else if (entries.Count > 0)
                {
                    TodayStatus = $"📝 進行中（{entries.Count}件入力済み）";
                }
                else
                {
                    TodayStatus = "🆕 開始済み（未入力）";
                }
            }
            else
            {
                HasTodaySession = false;
                TodayStatus = "❌ 未開始";
            }
        }

        private void StartToday()
        {
            var today = _db.GetOrCreateToday();
            _mainViewModel.CurrentView = new MindsetLabSessionViewModel(_mainViewModel, _db, today.Id);
        }

        private void ContinueToday()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var todayDay = _db.GetDayByDate(today);
            if (todayDay != null)
            {
                _mainViewModel.CurrentView = new MindsetLabSessionViewModel(_mainViewModel, _db, todayDay.Id);
            }
        }

        private void OpenDay(MsDay? day)
        {
            if (day == null) return;

            var review = _db.GetReviewByDay(day.Id);
            if (review != null)
            {
                // レビュー済みなら履歴表示
                _mainViewModel.CurrentView = new MindsetLabHistoryViewModel(_mainViewModel, _db, day.Id);
            }
            else
            {
                // 未完了ならセッション続行
                _mainViewModel.CurrentView = new MindsetLabSessionViewModel(_mainViewModel, _db, day.Id);
            }
        }
    }
}
