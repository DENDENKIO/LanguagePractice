using LanguagePractice.Helpers;
using LanguagePractice.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly SettingsService _settingsService;

        public ObservableCollection<AiSiteProfile> AiSites { get; }

        private AiSiteProfile? _selectedAiSite;
        public AiSiteProfile? SelectedAiSite
        {
            get => _selectedAiSite;
            set
            {
                _selectedAiSite = value;
                OnPropertyChanged();

                // 選択したらURLも自動で差し替え（ユーザーが手で編集してもOK）
                if (_selectedAiSite != null)
                {
                    AiUrl = _selectedAiSite.Url;
                    OnPropertyChanged(nameof(AutoModeHint));
                }
            }
        }

        private string _aiUrl = "";
        public string AiUrl
        {
            get => _aiUrl;
            set { _aiUrl = value; OnPropertyChanged(); }
        }

        private bool _isAutoMode;
        public bool IsAutoMode
        {
            get => _isAutoMode;
            set { _isAutoMode = value; OnPropertyChanged(); }
        }

        public string AutoModeHint
        {
            get
            {
                if (SelectedAiSite == null) return "";
                if (SelectedAiSite.SupportsAuto)
                    return "※このプリセットは自動操作を試行します（UI変更で失敗する場合あり）。";
                return "※このプリセットは自動操作が不安定/非推奨です。AUTO_MODEはOFF推奨（手動貼り付けで使用）。";
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand BackupDbCommand { get; }

        public SettingsViewModel()
        {
            _settingsService = new SettingsService();

            AiSites = new ObservableCollection<AiSiteProfile>(AiSiteCatalog.Presets);

            LoadSettings();

            SaveCommand = new RelayCommand(SaveSettings);
            BackupDbCommand = new RelayCommand(ExecuteBackup);
        }

        private void LoadSettings()
        {
            // 既存設定：サイトIDがあればそれを優先
            string siteId = _settingsService.GetValue("AI_SITE_ID", "GENSPARK");
            SelectedAiSite = AiSites.FirstOrDefault(x => x.Id == siteId) ?? AiSites.First();

            // URLはユーザーが編集している可能性もあるのでAI_URLを読む（無ければプリセット）
            AiUrl = _settingsService.GetValue("AI_URL", SelectedAiSite?.Url ?? AiSiteCatalog.Presets[0].Url);

            IsAutoMode = _settingsService.GetBoolean("AUTO_MODE", false);

            OnPropertyChanged(nameof(AutoModeHint));
        }

        private void SaveSettings()
        {
            if (SelectedAiSite != null)
                _settingsService.SaveValue("AI_SITE_ID", SelectedAiSite.Id);

            _settingsService.SaveValue("AI_URL", AiUrl);
            _settingsService.SaveBoolean("AUTO_MODE", IsAutoMode);

            MessageBox.Show("設定を保存しました。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteBackup()
        {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LanguagePractice",
                "lp_v1.db");

            if (!File.Exists(dbPath))
            {
                MessageBox.Show("データベースファイルが見つかりません。", "エラー");
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"LanguagePractice_Backup_{DateTime.Now:yyyyMMdd_HHmm}.db",
                DefaultExt = ".db",
                Filter = "SQLite Database (.db)|*.db"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.Copy(dbPath, dlg.FileName, true);
                    MessageBox.Show("バックアップを作成しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"バックアップに失敗しました: {ex.Message}", "エラー");
                }
            }
        }
    }
}
