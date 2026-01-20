using Dapper;
using System;

namespace LanguagePractice.Services
{
    public class SettingsService
    {
        public string GetValue(string key, string defaultValue = "")
        {
            using var conn = DatabaseService.GetConnection();
            string sql = "SELECT value FROM kv_settings WHERE key = @Key";
            var result = conn.QueryFirstOrDefault<string>(sql, new { Key = key });
            return result ?? defaultValue;
        }

        public void SaveValue(string key, string value)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = @"
                INSERT INTO kv_settings (key, value, updated_at) 
                VALUES (@Key, @Value, @UpdatedAt)
                ON CONFLICT(key) DO UPDATE SET
                    value = excluded.value,
                    updated_at = excluded.updated_at;";

            conn.Execute(sql, new { Key = key, Value = value, UpdatedAt = DateTime.Now.ToString("o") });
        }

        public bool GetBoolean(string key, bool defaultValue = false)
        {
            string val = GetValue(key, defaultValue ? "true" : "false");
            return bool.TryParse(val, out bool result) ? result : defaultValue;
        }

        public void SaveBoolean(string key, bool value)
        {
            SaveValue(key, value ? "true" : "false");
        }
    }
}
