using LanguagePractice.Helpers;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public ICommand ShowHomeCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ShowWorkbenchCommand { get; }
        public ICommand ShowLibraryCommand { get; }
        public ICommand ShowRoutesCommand { get; }
        public ICommand ShowPracticeCommand { get; }
        public ICommand ShowCompareCommand { get; }
        public ICommand ShowExperimentCommand { get; }
        public ICommand ShowPoetryLabCommand { get; }  // 追加

        public MainViewModel()
        {
            CurrentView = new SettingsViewModel();

            ShowHomeCommand = new RelayCommand(() => CurrentView = null);
            ShowSettingsCommand = new RelayCommand(() => CurrentView = new SettingsViewModel());
            ShowWorkbenchCommand = new RelayCommand(() => CurrentView = new WorkbenchViewModel());
            ShowLibraryCommand = new RelayCommand(() => CurrentView = new LibraryViewModel());
            ShowRoutesCommand = new RelayCommand(() => CurrentView = new RouteSelectionViewModel(this));
            ShowPracticeCommand = new RelayCommand(() => CurrentView = new PracticeSessionViewModel(this));
            ShowCompareCommand = new RelayCommand(() => CurrentView = new CompareViewModel());
            ShowExperimentCommand = new RelayCommand(() => CurrentView = new ExperimentViewModel());
            
            // PoetryLab追加
            ShowPoetryLabCommand = new RelayCommand(() => CurrentView = new PoetryLabHomeViewModel(this));
        }
    }
}
