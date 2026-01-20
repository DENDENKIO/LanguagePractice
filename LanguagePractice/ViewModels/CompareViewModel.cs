using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Dapper;

namespace LanguagePractice.ViewModels
{
    public class CompareViewModel : ViewModelBase
    {
        private readonly CompareService _compareService;

        // 比較対象の候補
        public ObservableCollection<Work> AvailableWorks { get; } = new ObservableCollection<Work>();

        // 左側
        private Work? _leftWork;
        public Work? LeftWork
        {
            get => _leftWork;
            set { _leftWork = value; OnPropertyChanged(); UpdateWinnerList(); }
        }

        // 右側
        private Work? _rightWork;
        public Work? RightWork
        {
            get => _rightWork;
            set { _rightWork = value; OnPropertyChanged(); UpdateWinnerList(); }
        }

        // 勝者選択用リスト
        public ObservableCollection<Work> WinnerCandidates { get; } = new ObservableCollection<Work>();

        private Work? _selectedWinner;
        public Work? SelectedWinner
        {
            get => _selectedWinner;
            set { _selectedWinner = value; OnPropertyChanged(); }
        }

        // 入力項目
        private string _compareTitle = "";
        public string CompareTitle { get => _compareTitle; set { _compareTitle = value; OnPropertyChanged(); } }

        private string _compareNote = "";
        public string CompareNote { get => _compareNote; set { _compareNote = value; OnPropertyChanged(); } }

        public ICommand SaveCommand { get; }

        public CompareViewModel()
        {
            _compareService = new CompareService();
            SaveCommand = new RelayCommand(ExecuteSave);
            LoadWorks();
        }

        private void LoadWorks()
        {
            try
            {
                using var conn = DatabaseService.GetConnection();
                // 直近50件を取得
                var works = conn.Query<Work>("SELECT * FROM work ORDER BY id DESC LIMIT 50").ToList();
                foreach (var w in works) AvailableWorks.Add(w);
            }
            catch { /* Ignore */ }
        }

        private void UpdateWinnerList()
        {
            WinnerCandidates.Clear();
            if (LeftWork != null) WinnerCandidates.Add(LeftWork);
            if (RightWork != null) WinnerCandidates.Add(RightWork);

            // 自動的にタイトル案を入れる
            if (string.IsNullOrEmpty(CompareTitle) && LeftWork != null && RightWork != null)
            {
                CompareTitle = $"{LeftWork.Title} vs {RightWork.Title}";
            }
        }

        private void ExecuteSave()
        {
            if (LeftWork == null || RightWork == null)
            {
                MessageBox.Show("比較する作品を2つ選んでください。", "入力不足");
                return;
            }
            if (string.IsNullOrWhiteSpace(CompareTitle))
            {
                MessageBox.Show("比較セットのタイトルを入力してください。", "入力不足");
                return;
            }

            try
            {
                var set = new CompareSet
                {
                    Title = CompareTitle,
                    Note = CompareNote,
                    WinnerWorkId = SelectedWinner?.Id,
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var items = new List<CompareItem>
                {
                    new CompareItem { WorkId = LeftWork.Id, Position = "Left" },
                    new CompareItem { WorkId = RightWork.Id, Position = "Right" }
                };

                _compareService.SaveComparison(set, items);

                MessageBox.Show("比較結果を保存しました！", "完了", MessageBoxButton.OK, MessageBoxImage.Information);

                // リセット
                CompareTitle = "";
                CompareNote = "";
                SelectedWinner = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存エラー: {ex.Message}");
            }
        }
    }
}
