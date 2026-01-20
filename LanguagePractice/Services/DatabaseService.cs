using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Dapper;

namespace LanguagePractice.Services
{
    public class DatabaseService
    {
        private static string DbPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "LanguagePractice",
            "lp_v1.db");

        private static string ConnectionString => $"Data Source={DbPath}";

        public DatabaseService()
        {
            InitializeDatabase();
        }

        public static void InitializeDatabase()
        {
            var directory = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            connection.Execute("PRAGMA foreign_keys = ON;");

            // 1. 設定
            connection.Execute("CREATE TABLE IF NOT EXISTS kv_settings (key TEXT PRIMARY KEY, value TEXT, updated_at TEXT);");

            // 2. 実行ログ
            connection.Execute("CREATE TABLE IF NOT EXISTS run_log (id INTEGER PRIMARY KEY AUTOINCREMENT, operation_kind TEXT NOT NULL, status TEXT NOT NULL, created_at TEXT NOT NULL, prompt_text TEXT, raw_output TEXT, error_code TEXT);");

            // 3. 作品
            connection.Execute("CREATE TABLE IF NOT EXISTS work (id INTEGER PRIMARY KEY AUTOINCREMENT, kind TEXT NOT NULL, title TEXT, body_text TEXT, created_at TEXT, run_log_id INTEGER, writer_name TEXT, reader_note TEXT, tone_label TEXT, FOREIGN KEY(run_log_id) REFERENCES run_log(id) ON DELETE SET NULL);");

            // 4. 学習カード
            connection.Execute("CREATE TABLE IF NOT EXISTS study_card (id INTEGER PRIMARY KEY AUTOINCREMENT, source_work_id INTEGER, created_at TEXT, focus TEXT, level TEXT, best_expressions_raw TEXT, metaphor_chains_raw TEXT, do_next_raw TEXT, tags TEXT, full_parsed_content TEXT, FOREIGN KEY(source_work_id) REFERENCES work(id) ON DELETE CASCADE);");

            // 5. カスタムルート
            connection.Execute("CREATE TABLE IF NOT EXISTS custom_route (id TEXT PRIMARY KEY, title TEXT NOT NULL, description TEXT, steps_json TEXT NOT NULL, updated_at TEXT);");

            // 6. ペルソナ
            connection.Execute("CREATE TABLE IF NOT EXISTS persona (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, location TEXT, bio TEXT, style TEXT, tags TEXT, verification_status TEXT, created_at TEXT);");

            // 7. 練習セッション
            connection.Execute("CREATE TABLE IF NOT EXISTS practice_session (id INTEGER PRIMARY KEY AUTOINCREMENT, pack_id TEXT, created_at TEXT, drill_a_memo TEXT, drill_b_metaphors TEXT, drill_c_draft TEXT, drill_c_core TEXT, drill_c_revision TEXT, wrap_best_one TEXT, wrap_todo TEXT, elapsed_seconds INTEGER, is_completed INTEGER);");

            // 8. Topic
            connection.Execute("CREATE TABLE IF NOT EXISTS topic (id INTEGER PRIMARY KEY AUTOINCREMENT, title TEXT, emotion TEXT, scene TEXT, tags TEXT, fix_conditions TEXT, created_at TEXT);");

            // 9. Observation
            connection.Execute("CREATE TABLE IF NOT EXISTS observation (id INTEGER PRIMARY KEY AUTOINCREMENT, image_url TEXT, motif TEXT, visual_raw TEXT, sound_raw TEXT, metaphors_raw TEXT, core_candidates_raw TEXT, full_content TEXT, created_at TEXT);");

            // 10. Compare Set
            connection.Execute("CREATE TABLE IF NOT EXISTS compare_set (id INTEGER PRIMARY KEY AUTOINCREMENT, title TEXT, note TEXT, winner_work_id INTEGER, created_at TEXT);");

            // 11. Compare Item
            connection.Execute("CREATE TABLE IF NOT EXISTS compare_item (id INTEGER PRIMARY KEY AUTOINCREMENT, compare_set_id INTEGER, work_id INTEGER, position TEXT, FOREIGN KEY(compare_set_id) REFERENCES compare_set(id) ON DELETE CASCADE);");

            // 12. Persona Verification
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS persona_verification (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    persona_id INTEGER,
                    created_at TEXT,
                    evidence1 TEXT,
                    evidence2 TEXT,
                    evidence3 TEXT,
                    result_json TEXT,
                    overall_verdict TEXT,
                    revised_bio_draft TEXT,
                    FOREIGN KEY(persona_id) REFERENCES persona(id) ON DELETE CASCADE
                );
            ");

            // 13. Experiment
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS experiment (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    title TEXT,
                    description TEXT,
                    created_at TEXT,
                    variable_name TEXT,
                    common_topic TEXT,
                    common_writer TEXT
                );
            ");

            // 14. Experiment Trial
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS experiment_trial (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    experiment_id INTEGER,
                    variable_value TEXT,
                    result_work_id INTEGER,
                    rating TEXT,
                    FOREIGN KEY(experiment_id) REFERENCES experiment(id) ON DELETE CASCADE
                );
            ");

            // ★ FTS5（全文検索）セットアップ
            EnsureFtsSchema(connection);
        }

        public static SqliteConnection GetConnection()
            => new SqliteConnection(ConnectionString);

        // ----------------------------
        // FTS5
        // ----------------------------
        private const string FtsSchemaVerKey = "FTS_SCHEMA_VER";
        private const string FtsSchemaVer = "3"; // 今回の版（前回の外部コンテンツ方式から変更）

        private static void EnsureFtsSchema(SqliteConnection conn)
        {
            // FTS5が無い環境でもアプリ起動を優先
            try
            {
                string currentVer = conn.ExecuteScalar<string?>(
                    "SELECT value FROM kv_settings WHERE key=@Key",
                    new { Key = FtsSchemaVerKey }
                ) ?? "";

                if (currentVer != FtsSchemaVer)
                {
                    DropFtsObjects(conn);
                    CreateAllFts(conn);

                    conn.Execute(@"
                        INSERT INTO kv_settings(key,value,updated_at)
                        VALUES(@K,@V,@U)
                        ON CONFLICT(key) DO UPDATE SET value=excluded.value, updated_at=excluded.updated_at;",
                        new { K = FtsSchemaVerKey, V = FtsSchemaVer, U = DateTime.Now.ToString("o") }
                    );
                }
                else
                {
                    // 念のため、存在しない場合は作る（途中でDBを消した等）
                    CreateAllFts(conn);
                }
            }
            catch
            {
                // no such module: fts5 等
            }
        }

        private static void DropFtsObjects(SqliteConnection conn)
        {
            // テーブル
            conn.Execute("DROP TABLE IF EXISTS work_fts;");
            conn.Execute("DROP TABLE IF EXISTS study_card_fts;");
            conn.Execute("DROP TABLE IF EXISTS persona_fts;");
            conn.Execute("DROP TABLE IF EXISTS topic_fts;");
            conn.Execute("DROP TABLE IF EXISTS observation_fts;");

            // トリガー
            conn.Execute("DROP TRIGGER IF EXISTS work_ai_fts;");
            conn.Execute("DROP TRIGGER IF EXISTS work_ad_fts;");
            conn.Execute("DROP TRIGGER IF EXISTS work_au_fts;");

            conn.Execute("DROP TRIGGER IF EXISTS study_card_ai_fts;");
            conn.Execute("DROP TRIGGER IF EXISTS study_card_ad_fts;");
            conn.Execute("DROP TRIGGER IF EXISTS study_card_au_fts;");

            conn.Execute("DROP TRIGGER IF EXISTS persona_ai_fts;");
            conn.Execute("DROP TRIGGER IF EXISTS persona_ad_fts;");
            conn.Execute("DROP TRIGGER IF EXISTS persona_au_fts;");

            conn.Execute("DROP TRIGGER IF EXISTS topic_ai_fts;");
            conn.Execute("DROP TRIGGER IF EXISTS topic_ad_fts;");
            conn.Execute("DROP TRIGGER IF EXISTS topic_au_fts;");

            conn.Execute("DROP TRIGGER IF EXISTS observation_ai_fts;");
            conn.Execute("DROP TRIGGER IF EXISTS observation_ad_fts;");
            conn.Execute("DROP TRIGGER IF EXISTS observation_au_fts;");
        }

        private static void CreateAllFts(SqliteConnection conn)
        {
            EnsureOneFts(
                conn,
                baseTable: "work",
                ftsTable: "work_fts",
                columns: new[] { "title", "body_text", "kind", "writer_name", "reader_note", "tone_label" }
            );

            EnsureOneFts(
                conn,
                baseTable: "study_card",
                ftsTable: "study_card_fts",
                columns: new[] { "focus", "level", "tags", "best_expressions_raw", "metaphor_chains_raw", "do_next_raw", "full_parsed_content" }
            );

            EnsureOneFts(
                conn,
                baseTable: "persona",
                ftsTable: "persona_fts",
                columns: new[] { "name", "location", "bio", "style", "tags", "verification_status" }
            );

            EnsureOneFts(
                conn,
                baseTable: "topic",
                ftsTable: "topic_fts",
                columns: new[] { "title", "emotion", "scene", "tags", "fix_conditions" }
            );

            EnsureOneFts(
                conn,
                baseTable: "observation",
                ftsTable: "observation_fts",
                columns: new[] { "image_url", "motif", "visual_raw", "sound_raw", "metaphors_raw", "core_candidates_raw", "full_content" }
            );
        }

        private static void EnsureOneFts(SqliteConnection conn, string baseTable, string ftsTable, string[] columns)
        {
            // tokenize='trigram' 優先（日本語部分一致に強い）→ だめなら unicode61
            string colDef = string.Join(", ", columns);

            string createTrigram = $@"
CREATE VIRTUAL TABLE IF NOT EXISTS {ftsTable}
USING fts5({colDef}, tokenize='trigram');";

            string createUnicode = $@"
CREATE VIRTUAL TABLE IF NOT EXISTS {ftsTable}
USING fts5({colDef}, tokenize='unicode61');";

            try { conn.Execute(createTrigram); }
            catch { conn.Execute(createUnicode); }

            // contentless なので、自前同期（lower(coalesce(...))でケース無視の索引化）
            string insertCols = string.Join(", ", columns);
            string newValues = string.Join(", ", columns.Select(c => $"lower(coalesce(new.{c},''))"));
            string oldId = "old.id";
            string deleteStmt = $"DELETE FROM {ftsTable} WHERE rowid = {oldId};";

            // INSERT
            conn.Execute($@"
CREATE TRIGGER IF NOT EXISTS {baseTable}_ai_fts
AFTER INSERT ON {baseTable}
BEGIN
  INSERT INTO {ftsTable}(rowid, {insertCols})
  VALUES (new.id, {newValues});
END;");

            // DELETE
            conn.Execute($@"
CREATE TRIGGER IF NOT EXISTS {baseTable}_ad_fts
AFTER DELETE ON {baseTable}
BEGIN
  {deleteStmt}
END;");

            // UPDATE
            conn.Execute($@"
CREATE TRIGGER IF NOT EXISTS {baseTable}_au_fts
AFTER UPDATE ON {baseTable}
BEGIN
  {deleteStmt}
  INSERT INTO {ftsTable}(rowid, {insertCols})
  VALUES (new.id, {newValues});
END;");

            // 初回インデックス構築（ftsが空なら投入）
            long baseCount = conn.ExecuteScalar<long>($"SELECT COUNT(1) FROM {baseTable};");
            long ftsCount = conn.ExecuteScalar<long>($"SELECT COUNT(1) FROM {ftsTable};");

            if (baseCount > 0 && ftsCount == 0)
            {
                string selectValues = string.Join(", ", columns.Select(c => $"lower(coalesce({c},''))"));
                conn.Execute($@"
INSERT INTO {ftsTable}(rowid, {insertCols})
SELECT id, {selectValues}
FROM {baseTable};");
            }
        }
    }
}
