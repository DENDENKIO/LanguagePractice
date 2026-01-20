using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System.Windows;

namespace LanguagePractice.ViewModels
{
    public class RouteSelectionViewModel : ViewModelBase
    {
        private readonly RouteService _presetService;
        private readonly CustomRouteService _customService;
        private readonly MainViewModel _mainViewModel;

        public ObservableCollection<RouteDefinition> Routes { get; } = new ObservableCollection<RouteDefinition>();

        public ICommand StartRouteCommand { get; }
        public ICommand CreateNewRouteCommand { get; }
        public ICommand EditRouteCommand { get; }
        public ICommand DeleteRouteCommand { get; }
        public ICommand ReloadCommand { get; }

        public RouteSelectionViewModel(MainViewModel mainVM)
        {
            _mainViewModel = mainVM;
            _presetService = new RouteService();
            _customService = new CustomRouteService();

            StartRouteCommand = new RelayCommand<RouteDefinition>(ExecuteStartRoute);
            CreateNewRouteCommand = new RelayCommand<object>(_ => ExecuteCreateNew());
            EditRouteCommand = new RelayCommand<RouteDefinition>(ExecuteEditRoute);
            DeleteRouteCommand = new RelayCommand<RouteDefinition>(ExecuteDeleteRoute);
            ReloadCommand = new RelayCommand<object>(_ => LoadRoutes());

            LoadRoutes();
        }

        public void LoadRoutes()
        {
            Routes.Clear();

            // 1. プリセット
            var presets = _presetService.GetPresets();
            foreach (var r in presets)
            {
                Routes.Add(r);
            }

            // 2. カスタム
            var customs = _customService.GetAllRoutes();
            foreach (var r in customs)
            {
                r.Description += " (Custom)";
                Routes.Add(r);
            }

            // デバッグ表示: 件数が正しく取れているか確認
            // MessageBox.Show($"一覧更新: プリセット{presets.Count}件, カスタム{customs.Count}件\n合計表示: {Routes.Count}件");
        }

        private void ExecuteStartRoute(RouteDefinition? route)
        {
            if (route == null) return;
            var executionVM = new RouteExecutionViewModel(route);
            _mainViewModel.CurrentView = executionVM;
        }

        private void ExecuteCreateNew()
        {
            var editorVM = new RouteEditorViewModel(_mainViewModel);
            _mainViewModel.CurrentView = editorVM;
        }

        private void ExecuteEditRoute(RouteDefinition? route)
        {
            if (route == null) return;

            if (_presetService.GetPresets().Any(p => p.Id == route.Id))
            {
                MessageBox.Show("プリセットルートは編集できません。");
                return;
            }

            var editorVM = new RouteEditorViewModel(_mainViewModel, route);
            _mainViewModel.CurrentView = editorVM;
        }

        private void ExecuteDeleteRoute(RouteDefinition? route)
        {
            if (route == null) return;

            if (_presetService.GetPresets().Any(p => p.Id == route.Id))
            {
                MessageBox.Show("プリセットルートは削除できません。");
                return;
            }

            if (MessageBox.Show($"ルート '{route.Title}' を削除しますか？", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _customService.DeleteRoute(route.Id);
                LoadRoutes();
            }
        }
    }
}
