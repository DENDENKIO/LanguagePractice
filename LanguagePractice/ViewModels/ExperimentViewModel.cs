using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using LanguagePractice.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    public class ExperimentViewModel : ViewModelBase
    {
        private readonly ExperimentService _service;
        private readonly PromptBuilder _promptBuilder;
        private readonly TopicService _topicService;
        private readonly PersonaService _personaService;

        public RunPanelViewModel RunPanelVM { get; }

        private string _expTitle = "読者像の違いによる比較実験"; public string ExpTitle { get => _expTitle; set { _expTitle = value; OnPropertyChanged(); } }
        private string _commonTopic = ""; public string CommonTopic { get => _commonTopic; set { _commonTopic = value; OnPropertyChanged(); } }
        private string _commonWriter = ""; public string CommonWriter { get => _commonWriter; set { _commonWriter = value; OnPropertyChanged(); } }

        private string _varValue1 = "疲れた社会人"; public string VarValue1 { get => _varValue1; set { _varValue1 = value; OnPropertyChanged(); } }
        private string _varValue2 = "文学好きの学生"; public string VarValue2 { get => _varValue2; set { _varValue2 = value; OnPropertyChanged(); } }
        private string _varValue3 = "未来の自分"; public string VarValue3 { get => _varValue3; set { _varValue3 = value; OnPropertyChanged(); } }

        private Experiment? _currentExperiment;
        public ObservableCollection<ExperimentTrialViewModel> Trials { get; } = new ObservableCollection<ExperimentTrialViewModel>();

        private int _currentTrialIndex = -1;

        public ICommand StartExperimentCommand { get; }
        public ICommand NextTrialCommand { get; }
        public ICommand PickCommonTopicCommand { get; }
        public ICommand PickCommonWriterCommand { get; }

        public ExperimentViewModel()
        {
            _service = new ExperimentService();
            _promptBuilder = new PromptBuilder();
            _topicService = new TopicService();
            _personaService = new PersonaService();
            RunPanelVM = new RunPanelViewModel();

            StartExperimentCommand = new RelayCommand(ExecuteStart);
            NextTrialCommand = new RelayCommand(ExecuteNextTrial);
            PickCommonTopicCommand = new RelayCommand(ExecutePickCommonTopic);
            PickCommonWriterCommand = new RelayCommand(ExecutePickCommonWriter);
        }

        private void ExecutePickCommonTopic()
        {
            var items = _topicService.GetRecentTopics();
            var dlg = new LibraryPickerDialog("共通お題を選択", items, PickerMode.Topic);
            if (dlg.ShowDialog() == true && dlg.SelectedItem is Topic t)
            {
                CommonTopic = t.Title;
            }
        }

        private void ExecutePickCommonWriter()
        {
            var items = _personaService.GetAllPersonas();
            var dlg = new LibraryPickerDialog("共通の書き手を選択", items, PickerMode.Persona);
            if (dlg.ShowDialog() == true && dlg.SelectedItem is Persona p)
            {
                CommonWriter = p.Name;
            }
        }

        private void ExecuteStart()
        {
            if (string.IsNullOrWhiteSpace(ExpTitle) || string.IsNullOrWhiteSpace(CommonTopic))
            {
                MessageBox.Show("タイトルとお題は必須です。", "入力不足");
                return;
            }

            var exp = new Experiment
            {
                Title = ExpTitle,
                Description = "TEXT_GENによる比較実験",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                VariableName = "Reader",
                CommonTopic = CommonTopic,
                CommonWriter = CommonWriter
            };

            var trialList = new List<ExperimentTrial>();
            if (!string.IsNullOrWhiteSpace(VarValue1)) trialList.Add(new ExperimentTrial { VariableValue = VarValue1 });
            if (!string.IsNullOrWhiteSpace(VarValue2)) trialList.Add(new ExperimentTrial { VariableValue = VarValue2 });
            if (!string.IsNullOrWhiteSpace(VarValue3)) trialList.Add(new ExperimentTrial { VariableValue = VarValue3 });

            if (trialList.Count == 0)
            {
                MessageBox.Show("比較条件（読者像）を少なくとも1つ入力してください。", "入力不足");
                return;
            }

            long expId = _service.CreateExperiment(exp, trialList);
            exp.Id = expId;
            _currentExperiment = exp;

            Trials.Clear();
            foreach (var t in _service.GetTrials(expId))
            {
                Trials.Add(new ExperimentTrialViewModel(t));
            }

            _currentTrialIndex = 0;
            StartTrial(_currentTrialIndex);
        }

        private void StartTrial(int index)
        {
            if (index >= Trials.Count)
            {
                MessageBox.Show("すべての実験が完了しました！", "完了");
                return;
            }

            var trialVM = Trials[index];
            trialVM.IsActive = true;

            string prompt = _promptBuilder.BuildTextGenPrompt(
                _currentExperiment!.CommonWriter,
                _currentExperiment.CommonTopic,
                trialVM.VariableValue,
                "",
                LengthProfile.STUDY_SHORT
            );

            RunPanelVM.Setup(OperationKind.TEXT_GEN, prompt);
        }

        private void ExecuteNextTrial()
        {
            long? savedId = RunPanelVM.GetLastSavedId();
            if (savedId == null)
            {
                if (MessageBox.Show("結果が保存されていませんが、次へ進みますか？", "確認", MessageBoxButton.YesNo) == MessageBoxResult.No) return;
            }

            var currentVM = Trials[_currentTrialIndex];
            currentVM.Trial.ResultWorkId = savedId;
            currentVM.IsActive = false;
            currentVM.IsCompleted = true;

            _service.UpdateTrial(currentVM.Trial);

            _currentTrialIndex++;
            StartTrial(_currentTrialIndex);
        }
    }

    public class ExperimentTrialViewModel : ViewModelBase
    {
        public ExperimentTrial Trial { get; }
        public string VariableValue => Trial.VariableValue;

        private bool _isActive;
        public bool IsActive { get => _isActive; set { _isActive = value; OnPropertyChanged(); } }

        private bool _isCompleted;
        public bool IsCompleted { get => _isCompleted; set { _isCompleted = value; OnPropertyChanged(); } }

        public ExperimentTrialViewModel(ExperimentTrial trial)
        {
            Trial = trial;
        }
    }
}
