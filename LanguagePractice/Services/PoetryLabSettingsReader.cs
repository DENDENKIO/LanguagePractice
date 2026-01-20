using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace LanguagePractice.Services
{
    /// <summary>
    /// 既存LanguagePracticeのSettings（lp_v1.db）からAI設定を読み取る
    /// </summary>
    public class PoetryLabSettingsReader
    {
        private static readonly string DbFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LanguagePractice");
        private static readonly string LpDbPath = Path.Combine(DbFolder, "lp_v1.db");

        /// <summary>
        /// AI_SITE_ID を取得
        /// </summary>
        public string GetAiSiteId()
        {
            return ReadSetting("AI_SITE_ID") ?? "GEMINI";
        }

        /// <summary>
        /// AI_URL を取得
        /// </summary>
        public string GetAiUrl()
        {
            return ReadSetting("AI_URL") ?? "";
        }

        /// <summary>
        /// AUTO_MODE が ON かどうか
        /// </summary>
        public bool IsAutoMode()
        {
            return ReadSetting("AUTO_MODE") == "ON";
        }

        /// <summary>
        /// 設定値を読み取る
        /// </summary>
        private string? ReadSetting(string key)
        {
            if (!File.Exists(LpDbPath))
            {
                return null;
            }

            try
            {
                using var conn = new SqliteConnection($"Data Source={LpDbPath}");
                conn.Open();

                // kv_settings テーブルが存在するか確認
                var tableExists = conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='kv_settings'"
                ) > 0;

                if (!tableExists)
                {
                    return null;
                }

                var sql = "SELECT value FROM kv_settings WHERE key = @Key";
                return conn.QueryFirstOrDefault<string>(sql, new { Key = key });
            }
            catch
            {
                return null;
            }
        }
    }
}
