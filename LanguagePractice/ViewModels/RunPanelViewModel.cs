using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using LanguagePractice.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    public class RunPanelViewModel : ViewModelBase
    {
        private readonly SettingsService _settingsService;
        private readonly RunLogService _runLogService;
        private readonly WorkService _workService;
        private readonly StudyCardService _studyCardService;
        private readonly PersonaService _personaService;
        private readonly TopicService _topicService;
        private readonly ObservationService _observationService;
        private readonly PersonaVerificationService _verifyService;

        private string _promptText = "";
        private string _userOutputText = "";
        private string _statusMessage = "準備完了";

        private long _currentRunId = 0;
        private OperationKind _currentOpKind;
        private long? _sourceWorkId = null;
        private string _imageUrlContext = "";
        private long? _targetPersonaId;

        private Dictionary<string, string>? _lastParsedDict;
        private long? _lastSavedRecordId;

        public string PromptText { get => _promptText; set { _promptText = value; OnPropertyChanged(); } }
        public string UserOutputText { get => _userOutputText; set { _userOutputText = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

        public ICommand CopyAndOpenAiCommand { get; }
        public ICommand FinishCommand { get; }

        public RunPanelViewModel()
        {
            _settingsService = new SettingsService();
            _runLogService = new RunLogService();
            _workService = new WorkService();
            _studyCardService = new StudyCardService();
            _personaService = new PersonaService();
            _topicService = new TopicService();
            _observationService = new ObservationService();
            _verifyService = new PersonaVerificationService();

            CopyAndOpenAiCommand = new RelayCommand(ExecuteCopyAndOpen);
            FinishCommand = new RelayCommand(ExecuteFinish);
        }

        public void Setup(OperationKind opKind, string prompt, long? sourceWorkId = null, string imageUrl = "", long? targetPersonaId = null)
        {
            _currentOpKind = opKind;
            PromptText = prompt;
            _sourceWorkId = sourceWorkId;
            _imageUrlContext = imageUrl;
            _targetPersonaId = targetPersonaId;
            UserOutputText = "";
            StatusMessage = "プロンプトが生成されました。AIサイトに送信してください。";
            _lastParsedDict = null;
            _lastSavedRecordId = null;
        }

        public Dictionary<string, string>? GetLastParsedOutput() => _lastParsedDict;
        public long? GetLastSavedId() => _lastSavedRecordId;

        private void ExecuteCopyAndOpen()
        {
            if (string.IsNullOrWhiteSpace(PromptText)) return;

            Clipboard.SetText(PromptText);

            var log = new RunLog
            {
                OperationKind = _currentOpKind.ToString(),
                Status = "RUNNING",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                PromptText = PromptText,
                RawOutput = null
            };
            _currentRunId = _runLogService.CreateLog(log);

            // ★追加：サイトIDも読む
            string siteId = _settingsService.GetValue("AI_SITE_ID", "GENSPARK");
            var profile = AiSiteCatalog.GetByIdOrDefault(siteId);

            // URLはユーザー編集を優先（無ければプリセット）
            string url = _settingsService.GetValue("AI_URL", profile.Url);

            bool isAutoMode = _settingsService.GetBoolean("AUTO_MODE", false);

            if (isAutoMode && !profile.SupportsAuto)
            {
                // 自動が非推奨なサイトなら注意（それでもやるならOK）
                StatusMessage = "注意：このサイトは自動操作が不安定です。手動貼り付けになる可能性があります。";
            }

            if (isAutoMode)
            {
                StatusMessage = "自動ブラウザモードで実行中...";
                var browser = new BrowserWindow(url, PromptText, profile.Id);

                if (browser.ShowDialog() == true)
                {
                    string result = browser.ResultText;

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        UserOutputText = result;
                        StatusMessage = "自動取得成功。解析を実行します...";
                        ExecuteFinish();
                    }
                    else
                    {
                        StatusMessage = "自動取得できませんでした。手動で貼り付けてください。";
                    }
                }
                else
                {
                    StatusMessage = "ブラウザ操作がキャンセルされました。";
                }
            }
            else
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    StatusMessage = "コピーしました。AIサイトを開きました。結果を貼り付けてください。";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"ブラウザ起動エラー: {ex.Message}";
                }
            }
        }

        private void ExecuteFinish()
        {
            if (string.IsNullOrWhiteSpace(UserOutputText))
            {
                MessageBox.Show("AIの出力を貼り付けてください。", "入力不足", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool parseSuccess = false;
            int savedCount = 0;

            if (_currentOpKind == OperationKind.TEXT_GEN)
            {
                var works = OutputParser.ParseWorks(UserOutputText, _currentRunId);
                parseSuccess = works.Count > 0;

                if (parseSuccess)
                {
                    foreach (var work in works)
                    {
                        long id = _workService.CreateWork(work);
                        _lastSavedRecordId ??= id;
                        savedCount++;
                    }

                    _lastParsedDict = OutputParser.Parse(UserOutputText, _currentOpKind);

                    StatusMessage = $"保存完了 ({savedCount}件のWork)";
                    MessageBox.Show($"{savedCount}件の作品(Work)を保存しました！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (_currentOpKind == OperationKind.STUDY_CARD)
            {
                var cards = OutputParser.ParseStudyCards(UserOutputText, _sourceWorkId);
                parseSuccess = cards.Count > 0;

                if (parseSuccess)
                {
                    foreach (var card in cards)
                    {
                        long id = _studyCardService.CreateStudyCard(card);
                        _lastSavedRecordId ??= id;
                        savedCount++;
                    }

                    _lastParsedDict = OutputParser.Parse(UserOutputText, _currentOpKind);

                    StatusMessage = $"保存完了 ({savedCount}件のStudyCard)";
                    MessageBox.Show($"{savedCount}件の学習カード(StudyCard)を保存しました！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (_currentOpKind == OperationKind.PERSONA_GEN)
            {
                var personas = OutputParser.ParsePersonas(UserOutputText);
                parseSuccess = personas.Count > 0;

                if (parseSuccess)
                {
                    foreach (var p in personas)
                    {
                        _personaService.CreatePersona(p);
                        savedCount++;
                    }

                    StatusMessage = $"保存完了 ({savedCount}件のPersona)";
                    MessageBox.Show($"{savedCount}人のペルソナを保存しました！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (_currentOpKind == OperationKind.TOPIC_GEN)
            {
                var topics = OutputParser.ParseTopics(UserOutputText);
                parseSuccess = topics.Count > 0;

                if (parseSuccess)
                {
                    foreach (var t in topics)
                    {
                        _topicService.CreateTopic(t);
                        savedCount++;
                    }

                    StatusMessage = $"保存完了 ({savedCount}件のTopic)";
                    MessageBox.Show($"{savedCount}件のお題(Topic)を保存しました！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (_currentOpKind == OperationKind.OBSERVE_IMAGE)
            {
                var parsedDict = OutputParser.Parse(UserOutputText, _currentOpKind);
                parseSuccess = parsedDict.Count > 0;

                if (parseSuccess)
                {
                    var obs = OutputParser.CreateObservationFromDict(parsedDict);
                    obs.ImageUrl = _imageUrlContext;
                    _lastSavedRecordId = _observationService.CreateObservation(obs);
                    _lastParsedDict = parsedDict;
                    savedCount = 1;

                    StatusMessage = "保存完了 (観察ノート)";
                    MessageBox.Show("観察ノート(Observation)を保存しました！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (_currentOpKind == OperationKind.CORE_EXTRACT)
            {
                var parsedDict = OutputParser.Parse(UserOutputText, _currentOpKind);
                parseSuccess = parsedDict.Count > 0;

                if (parseSuccess)
                {
                    var work = OutputParser.CreateWorkFromDict(parsedDict, _currentRunId);
                    work.Kind = WorkKind.ANALYSIS.ToString();
                    work.BodyText = UserOutputText;
                    _lastSavedRecordId = _workService.CreateWork(work);
                    _lastParsedDict = parsedDict;
                    savedCount = 1;

                    StatusMessage = "保存完了 (Core抽出)";
                    MessageBox.Show("Core抽出結果を保存しました！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (_currentOpKind == OperationKind.GIKO)
            {
                var parsedDict = OutputParser.Parse(UserOutputText, _currentOpKind);
                parseSuccess = parsedDict.Count > 0;

                if (parseSuccess)
                {
                    var work = OutputParser.CreateWorkFromDict(parsedDict, _currentRunId);
                    work.Kind = WorkKind.GIKO.ToString();
                    if (string.IsNullOrEmpty(work.BodyText))
                        work.BodyText = UserOutputText;

                    _lastSavedRecordId = _workService.CreateWork(work);
                    _lastParsedDict = parsedDict;
                    savedCount = 1;

                    StatusMessage = "保存完了 (擬古文)";
                    MessageBox.Show("擬古文を保存しました！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (_currentOpKind == OperationKind.REVISION_FULL)
            {
                var parsedDict = OutputParser.Parse(UserOutputText, _currentOpKind);
                parseSuccess = parsedDict.Count > 0;

                if (parseSuccess)
                {
                    var work = OutputParser.CreateWorkFromDict(parsedDict, _currentRunId);
                    work.Kind = WorkKind.REVISION.ToString();
                    work.BodyText = UserOutputText;
                    _lastSavedRecordId = _workService.CreateWork(work);
                    _lastParsedDict = parsedDict;
                    savedCount = 1;

                    StatusMessage = "保存完了 (推敲)";
                    MessageBox.Show("推敲結果(3案)を保存しました！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (_currentOpKind == OperationKind.PERSONA_VERIFY_ASSIST)
            {
                if (_targetPersonaId == null)
                {
                    MessageBox.Show("検証対象のPersona IDが不明です。保存できませんでした。", "警告");
                }
                else
                {
                    var verifyResult = OutputParser.ParseVerificationResult(UserOutputText, _targetPersonaId.Value, "", "", "");
                    _verifyService.SaveVerification(verifyResult);
                    parseSuccess = true;
                    savedCount = 1;

                    StatusMessage = "保存完了 (検証結果)";
                    MessageBox.Show($"検証結果を保存しました。\n判定: {verifyResult.OverallVerdict}", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                var parsedDict = OutputParser.Parse(UserOutputText, _currentOpKind);
                parseSuccess = parsedDict.Count > 0;
                _lastParsedDict = parsedDict;

                if (parseSuccess)
                {
                    StatusMessage = "解析成功（ログ保存のみ）";
                    MessageBox.Show("ログに保存しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            var logUpdate = new RunLog
            {
                Id = _currentRunId,
                Status = parseSuccess ? RunStatus.SUCCESS.ToString() : RunStatus.FAILED.ToString(),
                RawOutput = UserOutputText,
                ErrorCode = parseSuccess ? null : "ERR_PARSE_FAILED"
            };
            _runLogService.UpdateLog(logUpdate);

            if (!parseSuccess)
            {
                StatusMessage = "解析失敗";
                MessageBox.Show("解析に失敗しました。出力形式を確認してください。", "解析失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
