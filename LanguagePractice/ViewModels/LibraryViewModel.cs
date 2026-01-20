using LanguagePractice.Models;
using LanguagePractice.Services;
using LanguagePractice.Views;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LanguagePractice.Helpers;
using Dapper;

namespace LanguagePractice.ViewModels
{
    public class LibraryViewModel : ViewModelBase
    {
        private readonly WorkService _workService;
        private readonly StudyCardService _studyCardService;
        private readonly PersonaService _personaService;
        private readonly TopicService _topicService;
        private readonly ObservationService _observationService;

        public ObservableCollection<Work> Works { get; } = new ObservableCollection<Work>();
        public ObservableCollection<StudyCard> StudyCards { get; } = new ObservableCollection<StudyCard>();
        public ObservableCollection<Persona> Personas { get; } = new ObservableCollection<Persona>();
        public ObservableCollection<Topic> Topics { get; } = new ObservableCollection<Topic>();
        public ObservableCollection<Observation> Observations { get; } = new ObservableCollection<Observation>();

        private string _searchKeyword = "";
        public string SearchKeyword
        {
            get => _searchKeyword;
            set { _searchKeyword = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ShowDetailCommand { get; }
        public ICommand SearchCommand { get; }

        public LibraryViewModel()
        {
            _workService = new WorkService();
            _studyCardService = new StudyCardService();
            _personaService = new PersonaService();
            _topicService = new TopicService();
            _observationService = new ObservationService();

            RefreshCommand = new RelayCommand(LoadData);
            ShowDetailCommand = new RelayCommand<object>(ShowDetail);
            SearchCommand = new RelayCommand(ExecuteSearch);

            // ★保存後に自動更新
            LpUiBus.LibraryInvalidated += OnLibraryInvalidated;

            LoadData();
        }

        private void OnLibraryInvalidated()
        {
            // UIスレッドで更新
            Application.Current.Dispatcher.Invoke(() =>
            {
                LoadData();
            });
        }

        private void LoadData()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
                LoadAllLatest();
            else
                ExecuteSearch();
        }

        private void LoadAllLatest()
        {
            ClearAll();
            try
            {
                using var conn = DatabaseService.GetConnection();

                // id DESC を優先（created_at形式揺れに強い）
                foreach (var w in conn.Query<Work>("SELECT * FROM work ORDER BY id DESC LIMIT 100").ToList()) Works.Add(w);
                foreach (var c in conn.Query<StudyCard>("SELECT * FROM study_card ORDER BY id DESC LIMIT 100").ToList()) StudyCards.Add(c);
                foreach (var p in _personaService.GetAllPersonas()) Personas.Add(p);
                foreach (var t in conn.Query<Topic>("SELECT * FROM topic ORDER BY id DESC LIMIT 100").ToList()) Topics.Add(t);
                foreach (var o in conn.Query<Observation>("SELECT * FROM observation ORDER BY id DESC LIMIT 100").ToList()) Observations.Add(o);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"データ読み込みエラー: {ex.Message}");
            }
        }

        private void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                LoadAllLatest();
                return;
            }

            ClearAll();
            try
            {
                foreach (var w in _workService.SearchWorks(SearchKeyword)) Works.Add(w);
                foreach (var c in _studyCardService.SearchStudyCards(SearchKeyword)) StudyCards.Add(c);
                foreach (var p in _personaService.SearchPersonas(SearchKeyword)) Personas.Add(p);
                foreach (var t in _topicService.SearchTopics(SearchKeyword)) Topics.Add(t);
                foreach (var o in _observationService.SearchObservations(SearchKeyword)) Observations.Add(o);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"検索エラー: {ex.Message}");
            }
        }

        private void ClearAll()
        {
            Works.Clear();
            StudyCards.Clear();
            Personas.Clear();
            Topics.Clear();
            Observations.Clear();
        }

        // LibraryView code-behind から呼ぶ削除（以前のままでOK）
        public void DeleteItems(IList items)
        {
            if (items == null || items.Count == 0)
            {
                MessageBox.Show("削除する項目を選択してください。");
                return;
            }

            if (MessageBox.Show($"{items.Count} 件の項目を削除しますか？\nこの操作は元に戻せません。",
                "確認", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            var copied = items.Cast<object>().ToList();

            foreach (var item in copied)
            {
                if (item is Work w)
                {
                    _workService.DeleteWork(w.Id);
                    Works.Remove(w);
                }
                else if (item is StudyCard s)
                {
                    _studyCardService.DeleteStudyCard(s.Id);
                    StudyCards.Remove(s);
                }
                else if (item is Persona p)
                {
                    _personaService.DeletePersona(p.Id);
                    Personas.Remove(p);
                }
                else if (item is Topic t)
                {
                    _topicService.DeleteTopic(t.Id);
                    Topics.Remove(t);
                }
                else if (item is Observation o)
                {
                    _observationService.DeleteObservation(o.Id);
                    Observations.Remove(o);
                }
            }
        }

        private void ShowDetail(object? item)
        {
            if (item == null) return;

            if (item is Persona p)
            {
                var pWin = new PersonaDetailWindow(p);
                pWin.ShowDialog();
                return;
            }

            string title = "詳細";
            string content = "";

            if (item is Work w)
            {
                title = $"Work: {w.Title}";
                content = $"Kind: {w.Kind}\nWriter: {w.WriterName}\nReader: {w.ReaderNote}\nTone: {w.ToneLabel}\n\n{w.BodyText}";
            }
            else if (item is StudyCard s)
            {
                title = $"StudyCard (Source ID: {s.SourceWorkId})";
                content =
                    $"Focus: {s.Focus}\nLevel: {s.Level}\nTags: {s.Tags}\n\n" +
                    $"-- Best Expressions --\n{s.BestExpressionsRaw}\n\n" +
                    $"-- Metaphors --\n{s.MetaphorChainsRaw}\n\n" +
                    $"-- Next --\n{s.DoNextRaw}\n\n" +
                    $"-- Full Parse --\n{s.FullParsedContent}";
            }
            else if (item is Topic t)
            {
                title = $"Topic: {t.Title}";
                content = $"Emotion: {t.Emotion}\nScene: {t.Scene}\nTags: {t.Tags}\n\n-- Fix Conditions --\n{t.FixConditions}";
            }
            else if (item is Observation o)
            {
                title = $"Observation: {o.Motif}";
                content =
                    $"Image: {o.ImageUrl}\n\n" +
                    $"-- Visual --\n{o.VisualRaw}\n\n" +
                    $"-- Sound --\n{o.SoundRaw}\n\n" +
                    $"-- Metaphors --\n{o.MetaphorsRaw}\n\n" +
                    $"-- Core Candidates --\n{o.CoreCandidatesRaw}\n\n" +
                    $"-- Full --\n{o.FullContent}";
            }

            var win = new DetailWindow(title, content);
            win.ShowDialog();
        }
    }
}
