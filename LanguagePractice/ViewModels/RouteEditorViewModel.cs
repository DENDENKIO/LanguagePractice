using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    public class RouteEditorViewModel : ViewModelBase
    {
        private readonly CustomRouteService _service;
        private readonly MainViewModel _mainViewModel;

        private string _routeTitle = "新しいルート";
        public string RouteTitle { get => _routeTitle; set { _routeTitle = value; OnPropertyChanged(); } }

        private string _routeDescription = "";
        public string RouteDescription { get => _routeDescription; set { _routeDescription = value; OnPropertyChanged(); } }

        private string _routeId = "";

        public ObservableCollection<RouteStep> Steps { get; } = new ObservableCollection<RouteStep>();

        private RouteStep? _selectedStep;
        public RouteStep? SelectedStep
        {
            get => _selectedStep;
            set
            {
                if (_selectedStep != null)
                    _selectedStep.PropertyChanged -= SelectedStep_PropertyChanged;

                _selectedStep = value;

                if (_selectedStep != null)
                    _selectedStep.PropertyChanged += SelectedStep_PropertyChanged;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsStepSelected));
                OnPropertyChanged(nameof(IsGikoStepSelected));
                OnPropertyChanged(nameof(AvailableInputKeys));
                OnPropertyChanged(nameof(InputBindingsHint));

                LoadBindingRowsFromSelectedStep();
            }
        }

        private void SelectedStep_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RouteStep.Operation))
            {
                OnPropertyChanged(nameof(IsGikoStepSelected));
                OnPropertyChanged(nameof(AvailableInputKeys));
                OnPropertyChanged(nameof(InputBindingsHint));
            }
        }

        public bool IsStepSelected => SelectedStep != null;

        // ★GIKOのときだけToneプリセットUIを見せる
        public bool IsGikoStepSelected => SelectedStep?.Operation == OperationKind.GIKO;

        // --- Tone Presets (A方式) ---
        public ObservableCollection<TonePresetOption> TonePresets { get; } =
            new ObservableCollection<TonePresetOption>
            {
                new TonePresetOption { Label = "（なし / 自動）", Value = "" },
                new TonePresetOption { Label = "古典調（汎用）", Value = "古典調（汎用）" },
                new TonePresetOption { Label = "平安調", Value = "平安調" },
                new TonePresetOption { Label = "漢文訓読調", Value = "漢文訓読調" },
                new TonePresetOption { Label = "江戸戯作風", Value = "江戸戯作風" },
            };

        // --- InputBindings editor ---
        public ObservableCollection<InputBindingRow> BindingRows { get; } = new ObservableCollection<InputBindingRow>();

        private InputBindingRow? _selectedBindingRow;
        public InputBindingRow? SelectedBindingRow
        {
            get => _selectedBindingRow;
            set { _selectedBindingRow = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BindingValueMode> BindingModeList { get; }
            = new ObservableCollection<BindingValueMode>((BindingValueMode[])Enum.GetValues(typeof(BindingValueMode)));

        public ObservableCollection<string> AvailableInputKeys
        {
            get
            {
                var op = SelectedStep?.Operation ?? OperationKind.TEXT_GEN;
                return new ObservableCollection<string>(GetRecommendedInputKeys(op));
            }
        }

        public string InputBindingsHint
        {
            get
            {
                if (SelectedStep == null) return "";
                return SelectedStep.Operation switch
                {
                    OperationKind.TEXT_GEN => "例: TOPIC / READER / WRITER（空ならAIお任せ）",
                    OperationKind.STUDY_CARD => "例: SOURCE_TEXT（必須） / READER（任意）",
                    OperationKind.GIKO => "例: SOURCE_TEXT（必須） / TOPIC / READER / TONE_RULE（任意・FIXEDで上書き可）",
                    OperationKind.TOPIC_GEN => "例: IMAGE_URL（任意）",
                    OperationKind.OBSERVE_IMAGE => "例: IMAGE_URL（必須）",
                    OperationKind.CORE_EXTRACT => "例: SOURCE_TEXT（必須） / READER（任意）",
                    OperationKind.REVISION_FULL => "例: SOURCE_TEXT（必須） / CORE_SENTENCE（必須） / CORE_THEME 等",
                    OperationKind.PERSONA_GEN => "例: GENRE（任意）",
                    OperationKind.PERSONA_VERIFY_ASSIST => "例: PERSONA_NAME / PERSONA_BIO / EVIDENCE1..3",
                    _ => "このOperationは入力キー候補が未定義です（自由入力できます）。"
                };
            }
        }

        private bool _suppressBindingSync = false;

        public ICommand AddBindingCommand { get; }
        public ICommand RemoveBindingCommand { get; }

        public ICommand AddStepCommand { get; }
        public ICommand RemoveStepCommand { get; }
        public ICommand MoveStepUpCommand { get; }
        public ICommand MoveStepDownCommand { get; }
        public ICommand SaveRouteCommand { get; }
        public ICommand CancelCommand { get; }

        public ObservableCollection<OperationKind> OperationList { get; }

        public RouteEditorViewModel(MainViewModel mainVM, RouteDefinition? routeToEdit = null)
        {
            _mainViewModel = mainVM;
            _service = new CustomRouteService();

            OperationList = new ObservableCollection<OperationKind>((OperationKind[])Enum.GetValues(typeof(OperationKind)));

            AddStepCommand = new RelayCommand(ExecuteAddStep);
            RemoveStepCommand = new RelayCommand(ExecuteRemoveStep, () => IsStepSelected);
            MoveStepUpCommand = new RelayCommand(ExecuteMoveUp, () => IsStepSelected);
            MoveStepDownCommand = new RelayCommand(ExecuteMoveDown, () => IsStepSelected);
            SaveRouteCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            AddBindingCommand = new RelayCommand(ExecuteAddBinding, () => IsStepSelected);
            RemoveBindingCommand = new RelayCommand(ExecuteRemoveBinding, () => IsStepSelected && SelectedBindingRow != null);

            BindingRows.CollectionChanged += BindingRows_CollectionChanged;

            if (routeToEdit != null)
            {
                _routeId = routeToEdit.Id;
                RouteTitle = routeToEdit.Title;
                RouteDescription = routeToEdit.Description;
                foreach (var s in routeToEdit.Steps) Steps.Add(s);

                SelectedStep = Steps.FirstOrDefault();
            }
        }

        private static string[] GetRecommendedInputKeys(OperationKind op)
        {
            return op switch
            {
                OperationKind.TEXT_GEN => new[] { "TOPIC", "READER", "WRITER", "TONE" },
                OperationKind.STUDY_CARD => new[] { "SOURCE_TEXT", "READER", "TONE" },
                OperationKind.TOPIC_GEN => new[] { "IMAGE_URL" },
                OperationKind.PERSONA_GEN => new[] { "GENRE" },
                OperationKind.OBSERVE_IMAGE => new[] { "IMAGE_URL" },
                OperationKind.CORE_EXTRACT => new[] { "SOURCE_TEXT", "READER" },
                OperationKind.REVISION_FULL => new[] { "SOURCE_TEXT", "CORE_THEME", "CORE_EMOTION", "CORE_TAKEAWAY", "READER", "CORE_SENTENCE" },
                OperationKind.GIKO => new[] { "SOURCE_TEXT", "READER", "TOPIC", "TONE", "TONE_RULE" },
                OperationKind.PERSONA_VERIFY_ASSIST => new[] { "PERSONA_NAME", "PERSONA_BIO", "EVIDENCE1", "EVIDENCE2", "EVIDENCE3" },
                OperationKind.READER_AUTO_GEN => new[] { "CONTEXT_KIND" },
                _ => Array.Empty<string>()
            };
        }

        private void BindingRows_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var it in e.NewItems)
                    if (it is InputBindingRow row)
                        row.PropertyChanged += BindingRow_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (var it in e.OldItems)
                    if (it is InputBindingRow row)
                        row.PropertyChanged -= BindingRow_PropertyChanged;
            }

            SyncBindingRowsToSelectedStep();
        }

        private void BindingRow_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SyncBindingRowsToSelectedStep();
        }

        private void LoadBindingRowsFromSelectedStep()
        {
            _suppressBindingSync = true;
            try
            {
                BindingRows.Clear();
                SelectedBindingRow = null;

                if (SelectedStep == null) return;

                if (SelectedStep.InputBindings == null)
                    SelectedStep.InputBindings = new System.Collections.Generic.Dictionary<string, string>();

                foreach (var kv in SelectedStep.InputBindings)
                    BindingRows.Add(InputBindingRow.FromBinding(kv.Key, kv.Value));
            }
            finally
            {
                _suppressBindingSync = false;
            }

            SyncBindingRowsToSelectedStep();
        }

        private void SyncBindingRowsToSelectedStep()
        {
            if (_suppressBindingSync) return;
            if (SelectedStep == null) return;

            var dict = new System.Collections.Generic.Dictionary<string, string>();

            foreach (var row in BindingRows)
            {
                var key = (row.InputKey ?? "").Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;

                dict[key] = row.BindingString;
            }

            SelectedStep.InputBindings = dict;
        }

        private void ExecuteAddBinding()
        {
            if (SelectedStep == null) return;

            var keys = GetRecommendedInputKeys(SelectedStep.Operation);
            var suggestedKey = keys.FirstOrDefault() ?? "TOPIC";

            var row = new InputBindingRow
            {
                InputKey = suggestedKey,
                Mode = BindingValueMode.MANUAL,
                Value = ""
            };
            BindingRows.Add(row);
            SelectedBindingRow = row;
        }

        private void ExecuteRemoveBinding()
        {
            if (SelectedBindingRow == null) return;
            BindingRows.Remove(SelectedBindingRow);
            SelectedBindingRow = null;
        }

        private void ExecuteAddStep()
        {
            var newStep = new RouteStep
            {
                StepNumber = Steps.Count + 1,
                Title = $"Step {Steps.Count + 1}",
                Operation = OperationKind.TEXT_GEN,
                InputBindings = new System.Collections.Generic.Dictionary<string, string>(),
                FixedTone = ""
            };
            Steps.Add(newStep);
            SelectedStep = newStep;
        }

        private void ExecuteRemoveStep()
        {
            if (SelectedStep != null)
            {
                Steps.Remove(SelectedStep);
                SelectedStep = null;
                RenumberSteps();
            }
        }

        private void ExecuteMoveUp()
        {
            if (SelectedStep == null) return;
            int idx = Steps.IndexOf(SelectedStep);
            if (idx > 0)
            {
                Steps.Move(idx, idx - 1);
                RenumberSteps();
            }
        }

        private void ExecuteMoveDown()
        {
            if (SelectedStep == null) return;
            int idx = Steps.IndexOf(SelectedStep);
            if (idx < Steps.Count - 1)
            {
                Steps.Move(idx, idx + 1);
                RenumberSteps();
            }
        }

        private void RenumberSteps()
        {
            for (int i = 0; i < Steps.Count; i++)
                Steps[i].StepNumber = i + 1;
        }

        private void ExecuteSave()
        {
            if (string.IsNullOrWhiteSpace(RouteTitle))
            {
                MessageBox.Show("ルート名を入力してください。");
                return;
            }
            if (Steps.Count == 0)
            {
                MessageBox.Show("ステップを1つ以上追加してください。");
                return;
            }

            try
            {
                var stepsList = Steps.ToList();

                var route = new RouteDefinition
                {
                    Id = _routeId,
                    Title = RouteTitle,
                    Description = RouteDescription,
                    Steps = stepsList
                };

                _service.SaveRoute(route);

                MessageBox.Show("ルートを保存しました！", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
                _mainViewModel.CurrentView = new RouteSelectionViewModel(_mainViewModel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました。\n\n詳細: {ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel()
        {
            _mainViewModel.CurrentView = new RouteSelectionViewModel(_mainViewModel);
        }
    }
}
