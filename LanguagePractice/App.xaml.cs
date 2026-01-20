using System.Windows;
using Dapper;
using LanguagePractice.Services;

namespace LanguagePractice
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ★重要：snake_case（DB）→ PascalCase（C#）をDapperに許可する
            // 例: body_text -> BodyText, created_at -> CreatedAt, run_log_id -> RunLogId
            DefaultTypeMap.MatchNamesWithUnderscores = true;

            // DB初期化
            DatabaseService.InitializeDatabase();

            // メインウィンドウ表示（StartupUriは使わない構成に統一）
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
