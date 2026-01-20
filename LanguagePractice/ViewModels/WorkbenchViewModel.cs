using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using LanguagePractice.Views;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    public class WorkbenchViewModel : ViewModelBase
    {
        private readonly PromptBuilder _promptBuilder;
        private readonly TopicService _topicService;
        private readonly PersonaService _personaService;
        private readonly WorkService _workService;

        public RunPanelViewModel RunPanelVM { get; }

        private OperationKind _selectedOperation = OperationKind.TEXT_GEN;
        public OperationKind SelectedOperation
        {
            get => _selectedOperation;
            set
            {
                _selectedOperation = value;
                OnPropertyChanged();

                // 表示切替
                OnPropertyChanged(nameof(IsTextGenVisible));
                OnPropertyChanged(nameof(IsStudyCardVisible));
                OnPropertyChanged(nameof(IsPersonaGenVisible));
                OnPropertyChanged(nameof(IsPersonaVerifyVisible));
                OnPropertyChanged(nameof(IsTopicGenVisible));
                OnPropertyChanged(nameof(IsObserveImageVisible));
                OnPropertyChanged(nameof(IsCoreExtractVisible));
                OnPropertyChanged(nameof(IsRevisionFullVisible));
                OnPropertyChanged(nameof(IsGikoVisible));

                // ★追加：1行説明を更新
                OnPropertyChanged(nameof(OperationDescription));
            }
        }

        public bool IsTextGenVisible => SelectedOperation == OperationKind.TEXT_GEN;
        public bool IsStudyCardVisible => SelectedOperation == OperationKind.STUDY_CARD;
        public bool IsPersonaGenVisible => SelectedOperation == OperationKind.PERSONA_GEN;
        public bool IsPersonaVerifyVisible => SelectedOperation == OperationKind.PERSONA_VERIFY_ASSIST;
        public bool IsTopicGenVisible => SelectedOperation == OperationKind.TOPIC_GEN;
        public bool IsObserveImageVisible => SelectedOperation == OperationKind.OBSERVE_IMAGE;
        public bool IsCoreExtractVisible => SelectedOperation == OperationKind.CORE_EXTRACT;
        public bool IsRevisionFullVisible => SelectedOperation == OperationKind.REVISION_FULL;
        public bool IsGikoVisible => SelectedOperation == OperationKind.GIKO;

        public ObservableCollection<OperationKind> OperationList { get; }

        // ★追加：作業の目的（1行説明）
        public string OperationDescription => SelectedOperation switch
        {
            OperationKind.TEXT_GEN => "指定したお題・読者像・書き手から本文を生成し、作品(Work)として保存します。",
            OperationKind.STUDY_CARD => "貼り付けた本文を分解して学習カード(StudyCard)を作り、練習メニューとして保存します。",
            OperationKind.TOPIC_GEN => "作品づくりのための「詳細お題（固定条件つき）」を複数生成してTopicとして保存します。",
            OperationKind.PERSONA_GEN => "実在人物ベースの書き手像（Persona）を生成して、書き手候補として保存します。",
            OperationKind.OBSERVE_IMAGE => "画像から観察ノート（五感・比喩・核候補）を作り、Observationとして保存します。",
            OperationKind.CORE_EXTRACT => "本文の核（テーマ/感情/持ち帰り/核の一文）を抽出し、分析用Workとして保存します。",
            OperationKind.REVISION_FULL => "核を不変条件にして全文推敲（複数案）を作り、推敲Workとして保存します。",
            OperationKind.GIKO => "本文の意味を維持したまま指定文調へ書き換え、擬古文Workとして保存します。",
            OperationKind.PERSONA_VERIFY_ASSIST => "人物プロフィールの根拠テキストをもとに矛盾/支持を整理し、検証ログとして保存します。",
            OperationKind.READER_AUTO_GEN => "読者像（READER）を自動で1つ提案します（文章生成の前準備用）。",
            OperationKind.PRACTICE_SESSION => "練習セッション（時間管理つき）の実施用です。",
            _ => "選択した操作を実行して、結果をライブラリに保存します。"
        };

        // --- Input Fields ---
        private string _writerInput = "";
        public string WriterInput { get => _writerInput; set { _writerInput = value; OnPropertyChanged(); } }

        private string _topicInput = "";
        public string TopicInput { get => _topicInput; set { _topicInput = value; OnPropertyChanged(); } }

        private string _readerInput = "";
        public string ReaderInput { get => _readerInput; set { _readerInput = value; OnPropertyChanged(); } }

        private LengthProfile _selectedLength = LengthProfile.STUDY_SHORT;
        public LengthProfile SelectedLength { get => _selectedLength; set { _selectedLength = value; OnPropertyChanged(); } }

        public ObservableCollection<LengthProfile> LengthList { get; }

        private string _sourceTextInput = "";
        public string SourceTextInput { get => _sourceTextInput; set { _sourceTextInput = value; OnPropertyChanged(); } }

        private string _genreInput = "";
        public string GenreInput { get => _genreInput; set { _genreInput = value; OnPropertyChanged(); } }

        private string _imageUrlInput = "";
        public string ImageUrlInput { get => _imageUrlInput; set { _imageUrlInput = value; OnPropertyChanged(); } }

        private string _targetPersonaName = "";
        public string TargetPersonaName { get => _targetPersonaName; set { _targetPersonaName = value; OnPropertyChanged(); } }

        private string _targetPersonaBio = "";
        public string TargetPersonaBio { get => _targetPersonaBio; set { _targetPersonaBio = value; OnPropertyChanged(); } }

        private string _evidence1 = "";
        public string Evidence1 { get => _evidence1; set { _evidence1 = value; OnPropertyChanged(); } }

        private string _toneLabel = "";
        public string ToneLabel { get => _toneLabel; set { _toneLabel = value; OnPropertyChanged(); } }

        private string _toneRuleText = "";
        public string ToneRuleText { get => _toneRuleText; set { _toneRuleText = value; OnPropertyChanged(); } }

        private string _coreTheme = "";
        public string CoreTheme { get => _coreTheme; set { _coreTheme = value; OnPropertyChanged(); } }

        private string _coreEmotion = "";
        public string CoreEmotion { get => _coreEmotion; set { _coreEmotion = value; OnPropertyChanged(); } }

        private string _coreTakeaway = "";
        public string CoreTakeaway { get => _coreTakeaway; set { _coreTakeaway = value; OnPropertyChanged(); } }

        private string _coreSentence = "";
        public string CoreSentence { get => _coreSentence; set { _coreSentence = value; OnPropertyChanged(); } }

        public ICommand GeneratePromptCommand { get; }
        public ICommand PickTopicCommand { get; }
        public ICommand PickWriterCommand { get; }
        public ICommand PickTargetPersonaCommand { get; }
        public ICommand PickSourceTextCommand { get; }

        public WorkbenchViewModel()
        {
            _promptBuilder = new PromptBuilder();
            _topicService = new TopicService();
            _personaService = new PersonaService();
            _workService = new WorkService();
            RunPanelVM = new RunPanelViewModel();

            OperationList = new ObservableCollection<OperationKind>((OperationKind[])Enum.GetValues(typeof(OperationKind)));
            LengthList = new ObservableCollection<LengthProfile>((LengthProfile[])Enum.GetValues(typeof(LengthProfile)));

            GeneratePromptCommand = new RelayCommand(ExecuteGeneratePrompt);

            PickTopicCommand = new RelayCommand(ExecutePickTopic);
            PickWriterCommand = new RelayCommand(ExecutePickWriter);
            PickTargetPersonaCommand = new RelayCommand(ExecutePickTargetPersona);
            PickSourceTextCommand = new RelayCommand(ExecutePickSourceText);
        }

        private void ExecutePickTopic()
        {
            var items = _topicService.GetRecentTopics();
            var dlg = new LibraryPickerDialog("", items, PickerMode.Topic);
            if (dlg.ShowDialog() == true && dlg.SelectedItem is Topic t)
            {
                TopicInput = t.Title;
            }
        }

        private void ExecutePickWriter()
        {
            var items = _personaService.GetAllPersonas();
            var dlg = new LibraryPickerDialog("(Persona)", items, PickerMode.Persona);
            if (dlg.ShowDialog() == true && dlg.SelectedItem is Persona p)
            {
                WriterInput = p.Name;
            }
        }

        private void ExecutePickTargetPersona()
        {
            var items = _personaService.GetAllPersonas();
            var dlg = new LibraryPickerDialog("", items, PickerMode.Persona);
            if (dlg.ShowDialog() == true && dlg.SelectedItem is Persona p)
            {
                TargetPersonaName = p.Name;
                TargetPersonaBio = p.Bio;
            }
        }

        private void ExecutePickSourceText()
        {
            var items = _workService.GetRecentWorks();
            var dlg = new LibraryPickerDialog("", items, PickerMode.Work);
            if (dlg.ShowDialog() == true && dlg.SelectedItem is Work w)
            {
                SourceTextInput = w.BodyText;
            }
        }

        private void ExecuteGeneratePrompt()
        {
            string prompt = "";
            long? sourceWorkId = null;

            try
            {
                switch (SelectedOperation)
                {
                    case OperationKind.TEXT_GEN:
                        prompt = _promptBuilder.BuildTextGenPrompt(WriterInput, TopicInput, ReaderInput, "", SelectedLength);
                        break;

                    case OperationKind.STUDY_CARD:
                        if (string.IsNullOrWhiteSpace(SourceTextInput))
                        {
                            MessageBox.Show("対象本文を入力してください。");
                            return;
                        }
                        prompt = _promptBuilder.BuildStudyCardPrompt(ReaderInput, "", SourceTextInput);
                        break;

                    case OperationKind.PERSONA_GEN:
                        prompt = _promptBuilder.BuildPersonaGenPrompt(GenreInput);
                        break;

                    case OperationKind.TOPIC_GEN:
                        prompt = _promptBuilder.BuildTopicGenPrompt(ImageUrlInput);
                        break;

                    case OperationKind.OBSERVE_IMAGE:
                        if (string.IsNullOrWhiteSpace(ImageUrlInput))
                        {
                            MessageBox.Show("画像URLを入力してください。");
                            return;
                        }
                        prompt = _promptBuilder.BuildObserveImagePrompt(ImageUrlInput);
                        break;

                    case OperationKind.CORE_EXTRACT:
                        if (string.IsNullOrWhiteSpace(SourceTextInput))
                        {
                            MessageBox.Show("対象本文を入力してください。");
                            return;
                        }
                        prompt = _promptBuilder.BuildCoreExtractPrompt(ReaderInput, SourceTextInput);
                        break;

                    case OperationKind.GIKO:
                        if (string.IsNullOrWhiteSpace(SourceTextInput))
                        {
                            MessageBox.Show("元の現代文（対象本文）を入力してください。");
                            return;
                        }
                        prompt = _promptBuilder.BuildGikoPrompt(ToneLabel, ToneRuleText, ReaderInput, TopicInput, SourceTextInput);
                        break;

                    case OperationKind.REVISION_FULL:
                        if (string.IsNullOrWhiteSpace(SourceTextInput))
                        {
                            MessageBox.Show("元原稿（対象本文）を入力してください。");
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(CoreSentence))
                        {
                            MessageBox.Show("核の一文（CoreSentence）を入力してください。");
                            return;
                        }
                        prompt = _promptBuilder.BuildRevisionFullPrompt(SourceTextInput, CoreTheme, CoreEmotion, CoreTakeaway, ReaderInput, CoreSentence);
                        break;

                    case OperationKind.PERSONA_VERIFY_ASSIST:
                        prompt = _promptBuilder.BuildPersonaVerifyPrompt(TargetPersonaName, TargetPersonaBio, Evidence1, "", "");
                        break;

                    default:
                        MessageBox.Show("未対応のOperationです。");
                        return;
                }

                RunPanelVM.Setup(SelectedOperation, prompt, sourceWorkId, ImageUrlInput);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"プロンプト生成エラー: {ex.Message}");
            }
        }
    }
}
