using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace LanguagePractice.ViewModels
{
    public class PracticeSessionViewModel : ViewModelBase
    {
        private readonly PracticeService _service;
        private readonly MainViewModel _mainViewModel;
        private readonly DispatcherTimer _timer;

        public PracticeSession? Session { get; private set; }

        // 状態管理
        private bool _isSelectionMode = true;
        public bool IsSelectionMode { get => _isSelectionMode; set { _isSelectionMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsRunningMode)); } }
        public bool IsRunningMode => !IsSelectionMode;

        // パック選択用
        public ObservableCollection<PracticeService.PracticePackInfo> AvailablePacks { get; }

        private PracticeService.PracticePackInfo? _selectedPack;
        public PracticeService.PracticePackInfo? SelectedPack
        {
            get => _selectedPack;
            set { _selectedPack = value; OnPropertyChanged(); }
        }

        // ドリルステップ
        private int _currentStepIndex = 0;
        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set
            {
                _currentStepIndex = value;
                OnPropertyChanged();
                // 画面表示更新
                OnPropertyChanged(nameof(StepTitle));
                OnPropertyChanged(nameof(StepDescription));
                OnPropertyChanged(nameof(DrillA_Header));
                OnPropertyChanged(nameof(DrillB_Header));
                OnPropertyChanged(nameof(DrillC_DraftHeader));

                OnPropertyChanged(nameof(IsDrillA));
                OnPropertyChanged(nameof(IsDrillB));
                OnPropertyChanged(nameof(IsDrillC));
                OnPropertyChanged(nameof(IsWrap));
                OnPropertyChanged(nameof(IsNotWrap));
            }
        }

        public bool IsDrillA => CurrentStepIndex == 0;
        public bool IsDrillB => CurrentStepIndex == 1;
        public bool IsDrillC => CurrentStepIndex == 2;
        public bool IsWrap => CurrentStepIndex == 3;
        public bool IsNotWrap => !IsWrap;

        // --- 動的テキスト群 ---

        // パックIDを取得するヘルパー
        private string CurrentPackId => Session?.PackId ?? "POET_BASIC_1";

        public string StepTitle
        {
            get
            {
                if (CurrentPackId == "REVISION_FOCUS")
                {
                    switch (CurrentStepIndex)
                    {
                        case 0: return "Step 1: 現状分析・読み込み (10分)";
                        case 1: return "Step 2: 問題点の洗い出し (5分)";
                        case 2: return "Step 3: 改稿・再構成 (13分)";
                        case 3: return "Wrap: まとめ (2分)";
                    }
                }
                else if (CurrentPackId == "SOUND_RHYTHM")
                {
                    switch (CurrentStepIndex)
                    {
                        case 0: return "Step 1: 音読・リズム確認 (5分)";
                        case 1: return "Step 2: 母音・子音の響き (5分)";
                        case 2: return "Step 3: 整音・リライト (8分)";
                        case 3: return "Wrap: まとめ (2分)";
                    }
                }

                // Default (POET_BASIC_1)
                switch (CurrentStepIndex)
                {
                    case 0: return "Step 1: 五感メモ / 準備 (15分)";
                    case 1: return "Step 2: 比喩・素材出し (15分)";
                    case 2: return "Step 3: 執筆・推敲 (28分)";
                    case 3: return "Wrap: まとめ (2分)";
                }
                return "";
            }
        }

        public string StepDescription
        {
            get
            {
                if (CurrentPackId == "REVISION_FOCUS")
                {
                    switch (CurrentStepIndex)
                    {
                        case 0: return "既存の文章を読み込み、違和感や改善点をメモしましょう。";
                        case 1: return "具体的に削る箇所、動かす箇所、足す箇所をリストアップします。";
                        case 2: return "核（Core）を再確認し、実際に文章を書き直しましょう。";
                        case 3: return "改稿の成果を確認し、保存しましょう。";
                    }
                }
                else if (CurrentPackId == "SOUND_RHYTHM")
                {
                    switch (CurrentStepIndex)
                    {
                        case 0: return "声に出して読み、つっかえる箇所や息継ぎの位置を確認します。";
                        case 1: return "同音の重なりや、響きの硬さ・柔らかさを分析します。";
                        case 2: return "リズムを整えるために語順や語彙を変更します。";
                        case 3: return "整った文章を保存しましょう。";
                    }
                }

                // Default
                switch (CurrentStepIndex)
                {
                    case 0: return "テーマを決め、視覚・聴覚・嗅覚・触覚・体感で感じたことを箇条書きでメモしましょう。";
                    case 1: return "メモから比喩（〜のようだ）を5つ、そこから感情へ変容させる表現を3つ作りましょう。";
                    case 2: return "素材を使って初稿（250-450字）を書き、核（Core）を決めて削る・動かす・足す推敲を行いましょう。";
                    case 3: return "今日のベスト1文を選び、明日の課題を1行で書き残して終了です。";
                }
                return "";
            }
        }

        // 入力欄ラベルの動的変更
        public string DrillA_Header => CurrentPackId switch
        {
            "REVISION_FOCUS" => "現状分析メモ (Analysis)",
            "SOUND_RHYTHM" => "リズム確認メモ (Rhythm Check)",
            _ => "五感メモ (Sensory Notes)"
        };

        public string DrillB_Header => CurrentPackId switch
        {
            "REVISION_FOCUS" => "修正点リスト (Fix List)",
            "SOUND_RHYTHM" => "響き・音韻分析 (Sound Analysis)",
            _ => "比喩と変容 (Metaphors & Transforms)"
        };

        public string DrillC_DraftHeader => CurrentPackId switch
        {
            "REVISION_FOCUS" => "改稿案 (Revised Draft)",
            "SOUND_RHYTHM" => "整音案 (Polished Draft)",
            _ => "初稿 (Draft)"
        };

        public string TimerText => Session != null ? TimeSpan.FromSeconds(Session.ElapsedSeconds).ToString(@"mm\:ss") : "00:00";

        // Commands
        public ICommand StartSessionCommand { get; }
        public ICommand NextStepCommand { get; }
        public ICommand FinishCommand { get; }
        public ICommand SaveDraftCommand { get; }

        public PracticeSessionViewModel(MainViewModel mainVM)
        {
            _mainViewModel = mainVM;
            _service = new PracticeService();
            AvailablePacks = new ObservableCollection<PracticeService.PracticePackInfo>(_service.GetAvailablePacks());

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            StartSessionCommand = new RelayCommand(ExecuteStartSession);
            NextStepCommand = new RelayCommand(ExecuteNextStep);
            FinishCommand = new RelayCommand(ExecuteFinish);
            SaveDraftCommand = new RelayCommand(ExecuteSave);
        }

        private void ExecuteStartSession()
        {
            if (SelectedPack == null)
            {
                MessageBox.Show("練習コースを選択してください。");
                return;
            }

            Session = new PracticeSession
            {
                PackId = SelectedPack.Id,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            Session.Id = _service.CreateSession(Session);

            IsSelectionMode = false;
            CurrentStepIndex = 0;

            _timer.Start();
            OnPropertyChanged(nameof(Session));

            // 開始直後にタイトル等を更新
            OnPropertyChanged(nameof(StepTitle));
            OnPropertyChanged(nameof(StepDescription));
            OnPropertyChanged(nameof(DrillA_Header));
            OnPropertyChanged(nameof(DrillB_Header));
            OnPropertyChanged(nameof(DrillC_DraftHeader));
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (Session != null)
            {
                Session.ElapsedSeconds++;
                OnPropertyChanged(nameof(TimerText));
            }
        }

        private void ExecuteSave()
        {
            if (Session != null) _service.UpdateSession(Session);
        }

        private void ExecuteNextStep()
        {
            ExecuteSave();
            if (CurrentStepIndex < 3)
            {
                CurrentStepIndex++;
            }
        }

        private void ExecuteFinish()
        {
            if (Session != null)
            {
                Session.IsCompleted = true;
                ExecuteSave();
            }
            _timer.Stop();

            MessageBox.Show("お疲れ様でした！セッションを保存しました。", "終了", MessageBoxButton.OK, MessageBoxImage.Information);
            _mainViewModel.ShowLibraryCommand.Execute(null);
        }
    }
}
