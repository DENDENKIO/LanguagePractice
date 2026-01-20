using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    /// <summary>
    /// PoetryLab ホーム画面 ViewModel
    /// </summary>
    public class PoetryLabHomeViewModel : ViewModelBase
    {
        private readonly PoetryDatabaseService _db;
        private readonly PoetryProjectService _projectService;
        private readonly MainViewModel _mainViewModel;

        private ObservableCollection<PlProject> _projects = new();
        public ObservableCollection<PlProject> Projects
        {
            get => _projects;
            set { _projects = value; OnPropertyChanged(); }
        }

        private PlProject? _selectedProject;
        public PlProject? SelectedProject
        {
            get => _selectedProject;
            set { _selectedProject = value; OnPropertyChanged(); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterProjects(); }
        }

        private string _selectedStyleFilter = "全て";
        public string SelectedStyleFilter
        {
            get => _selectedStyleFilter;
            set { _selectedStyleFilter = value; OnPropertyChanged(); FilterProjects(); }
        }

        public string[] StyleFilters => new[] { "全て", "KOU", "BU", "MIX" };

        // 新規作成ダイアログ用
        private bool _isCreateDialogOpen;
        public bool IsCreateDialogOpen
        {
            get => _isCreateDialogOpen;
            set { _isCreateDialogOpen = value; OnPropertyChanged(); }
        }

        private string _newProjectTitle = string.Empty;
        public string NewProjectTitle
        {
            get => _newProjectTitle;
            set { _newProjectTitle = value; OnPropertyChanged(); }
        }

        private string _newProjectStyle = "KOU";
        public string NewProjectStyle
        {
            get => _newProjectStyle;
            set { _newProjectStyle = value; OnPropertyChanged(); }
        }

        public string[] StyleOptions => new[] { "KOU", "BU", "MIX" };

        private List<PlProject> _allProjects = new();

        // コマンド
        public ICommand OpenCreateDialogCommand { get; }
        public ICommand CreateProjectCommand { get; }
        public ICommand CancelCreateCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand DeleteProjectCommand { get; }
        public ICommand RefreshCommand { get; }

        public PoetryLabHomeViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _db = new PoetryDatabaseService();
            _projectService = new PoetryProjectService(_db);

            OpenCreateDialogCommand = new RelayCommand(OpenCreateDialog);
            CreateProjectCommand = new RelayCommand(CreateProject, CanCreateProject);
            CancelCreateCommand = new RelayCommand(CancelCreate);
            OpenProjectCommand = new RelayCommand<PlProject>(OpenProject);
            DeleteProjectCommand = new RelayCommand<PlProject>(DeleteProject);
            RefreshCommand = new RelayCommand(LoadProjects);

            LoadProjects();
        }

        private void LoadProjects()
        {
            _allProjects = _projectService.GetAll();
            FilterProjects();
        }

        private void FilterProjects()
        {
            var filtered = _allProjects.AsEnumerable();

            // スタイルフィルター
            if (_selectedStyleFilter != "全て")
            {
                filtered = filtered.Where(p => p.StyleType == _selectedStyleFilter);
            }

            // 検索フィルター
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var search = _searchText.ToLower();
                filtered = filtered.Where(p => p.Title.ToLower().Contains(search));
            }

            Projects = new ObservableCollection<PlProject>(filtered);
        }

        private void OpenCreateDialog()
        {
            NewProjectTitle = string.Empty;
            NewProjectStyle = "KOU";
            IsCreateDialogOpen = true;
        }

        private bool CanCreateProject()
        {
            return !string.IsNullOrWhiteSpace(NewProjectTitle);
        }

        private void CreateProject()
        {
            if (string.IsNullOrWhiteSpace(NewProjectTitle)) return;

            var project = _projectService.Create(NewProjectTitle.Trim(), NewProjectStyle);
            IsCreateDialogOpen = false;

            // 作成後、プロジェクト画面へ遷移
            _mainViewModel.CurrentView = new PoetryLabProjectViewModel(_mainViewModel, _db, project.Id);
        }

        private void CancelCreate()
        {
            IsCreateDialogOpen = false;
        }

        private void OpenProject(PlProject? project)
        {
            if (project == null) return;
            _mainViewModel.CurrentView = new PoetryLabProjectViewModel(_mainViewModel, _db, project.Id);
        }

        private void DeleteProject(PlProject? project)
        {
            if (project == null) return;

            // 確認は View 側で行う想定
            _projectService.Delete(project.Id);
            LoadProjects();
        }
    }
}
