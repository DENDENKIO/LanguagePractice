using Dapper;
using LanguagePractice.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LanguagePractice.Services
{
    /// <summary>
    /// MindsetLab専用データベースサービス
    /// DB: mindset_v1.db（別ファイル）
    /// </summary>
    public class MindsetDatabaseService
    {
        private static readonly string DbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "LanguagePractice");

        private static readonly string DbPath = Path.Combine(DbFolder, "mindset_v1.db");

        public static SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            return conn;
        }

        public static void InitializeDatabase()
        {
            if (!Directory.Exists(DbFolder))
            {
                Directory.CreateDirectory(DbFolder);
            }

            using var conn = GetConnection();

            // ms_profile
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS ms_profile (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT,
                    created_at TEXT NOT NULL
                )");

            // ms_day
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS ms_day (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    date_key TEXT NOT NULL UNIQUE,
                    focus_mindsets TEXT,
                    scene TEXT,
                    start_ritual TEXT,
                    end_ritual TEXT,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )");

            // ms_entry
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS ms_entry (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    day_id INTEGER NOT NULL,
                    entry_type TEXT NOT NULL,
                    body_text TEXT,
                    created_at TEXT NOT NULL,
                    FOREIGN KEY (day_id) REFERENCES ms_day(id)
                )");

            // ms_ai_step_log
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS ms_ai_step_log (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    day_id INTEGER NOT NULL,
                    step_name TEXT NOT NULL,
                    prompt_text TEXT,
                    raw_output TEXT,
                    parsed_json TEXT,
                    status TEXT DEFAULT 'RUNNING',
                    created_at TEXT NOT NULL,
                    finished_at TEXT,
                    FOREIGN KEY (day_id) REFERENCES ms_day(id)
                )");

            // ms_review
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS ms_review (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    day_id INTEGER NOT NULL UNIQUE,
                    total_score INTEGER,
                    subscores_json TEXT,
                    strengths TEXT,
                    weaknesses TEXT,
                    next_day_plan TEXT,
                    core_link TEXT,
                    created_at TEXT NOT NULL,
                    FOREIGN KEY (day_id) REFERENCES ms_day(id)
                )");

            // ms_export_log
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS ms_export_log (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    day_id INTEGER NOT NULL,
                    file_path TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    FOREIGN KEY (day_id) REFERENCES ms_day(id)
                )");

            // インデックス
            conn.Execute("CREATE INDEX IF NOT EXISTS idx_ms_day_date ON ms_day(date_key)");
            conn.Execute("CREATE INDEX IF NOT EXISTS idx_ms_entry_day ON ms_entry(day_id)");
            conn.Execute("CREATE INDEX IF NOT EXISTS idx_ms_ai_step_log_day ON ms_ai_step_log(day_id)");

            // ========== 既存DB対応: カラム追加 ==========
            TryAddColumn(conn, "ms_day", "scene", "TEXT");
            TryAddColumn(conn, "ms_ai_step_log", "finished_at", "TEXT");
        }

        /// <summary>
        /// カラムが存在しない場合に追加（既存DB対応）
        /// </summary>
        private static void TryAddColumn(SqliteConnection conn, string tableName, string columnName, string columnType)
        {
            try
            {
                conn.Execute($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}");
            }
            catch
            {
                // カラムが既に存在する場合は無視
            }
        }

        // ========== Day ==========

        public MsDay? GetDay(int id)
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<MsDay>("SELECT * FROM ms_day WHERE id = @Id", new { Id = id });
        }

        public MsDay? GetDayByDate(string dateKey)
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<MsDay>("SELECT * FROM ms_day WHERE date_key = @DateKey", new { DateKey = dateKey });
        }

        public int CreateDay(string dateKey)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("o");
            return conn.QuerySingle<int>(@"
                INSERT INTO ms_day (date_key, created_at, updated_at)
                VALUES (@DateKey, @Now, @Now);
                SELECT last_insert_rowid();",
                new { DateKey = dateKey, Now = now });
        }

        public int GetOrCreateDay(string dateKey)
        {
            var existing = GetDayByDate(dateKey);
            if (existing != null) return existing.Id;
            return CreateDay(dateKey);
        }

        /// <summary>
        /// 今日の日付でDayを取得または作成
        /// </summary>
        public int GetOrCreateToday()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            return GetOrCreateDay(today);
        }

        public void UpdateDay(int dayId, string focusMindsets, string? scene, string? startRitual, string? endRitual)
        {
            using var conn = GetConnection();
            conn.Execute(@"
                UPDATE ms_day 
                SET focus_mindsets = @FocusMindsets,
                    scene = @Scene,
                    start_ritual = @StartRitual, 
                    end_ritual = @EndRitual,
                    updated_at = @UpdatedAt
                WHERE id = @Id",
                new
                {
                    Id = dayId,
                    FocusMindsets = focusMindsets,
                    Scene = scene,
                    StartRitual = startRitual,
                    EndRitual = endRitual,
                    UpdatedAt = DateTime.Now.ToString("o")
                });
        }

        public List<MsDay> GetRecentDays(int limit = 30)
        {
            using var conn = GetConnection();
            return conn.Query<MsDay>(
                "SELECT * FROM ms_day ORDER BY date_key DESC LIMIT @Limit",
                new { Limit = limit }).ToList();
        }

        public List<MsDay> GetAllDays()
        {
            using var conn = GetConnection();
            return conn.Query<MsDay>("SELECT * FROM ms_day ORDER BY date_key DESC").ToList();
        }

        public int GetConsecutiveDays()
        {
            var days = GetRecentDays(100);
            if (days.Count == 0) return 0;

            int count = 0;
            var checkDate = DateTime.Today;

            foreach (var day in days.OrderByDescending(d => d.DateKey))
            {
                if (day.DateKey == checkDate.ToString("yyyy-MM-dd"))
                {
                    count++;
                    checkDate = checkDate.AddDays(-1);
                }
                else if (day.DateKey == checkDate.AddDays(1).ToString("yyyy-MM-dd"))
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            return count;
        }

        // ========== Entry ==========

        public List<MsEntry> GetEntriesByDay(int dayId)
        {
            using var conn = GetConnection();
            return conn.Query<MsEntry>(
                "SELECT * FROM ms_entry WHERE day_id = @DayId ORDER BY entry_type",
                new { DayId = dayId }).ToList();
        }

        public MsEntry? GetEntry(int dayId, string entryType)
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<MsEntry>(
                "SELECT * FROM ms_entry WHERE day_id = @DayId AND entry_type = @EntryType",
                new { DayId = dayId, EntryType = entryType });
        }

        public void UpsertEntry(int dayId, string entryType, string bodyText)
        {
            using var conn = GetConnection();
            var existing = GetEntry(dayId, entryType);
            var now = DateTime.Now.ToString("o");

            if (existing != null)
            {
                conn.Execute(@"
                    UPDATE ms_entry SET body_text = @BodyText WHERE id = @Id",
                    new { Id = existing.Id, BodyText = bodyText });
            }
            else
            {
                conn.Execute(@"
                    INSERT INTO ms_entry (day_id, entry_type, body_text, created_at)
                    VALUES (@DayId, @EntryType, @BodyText, @Now)",
                    new { DayId = dayId, EntryType = entryType, BodyText = bodyText, Now = now });
            }
        }

        public void DeleteEntry(int entryId)
        {
            using var conn = GetConnection();
            conn.Execute("DELETE FROM ms_entry WHERE id = @Id", new { Id = entryId });
        }

        // ========== AiStepLog ==========

        public int CreateAiStepLog(int dayId, string stepName, string promptText)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("o");
            return conn.QuerySingle<int>(@"
                INSERT INTO ms_ai_step_log (day_id, step_name, prompt_text, status, created_at)
                VALUES (@DayId, @StepName, @PromptText, 'RUNNING', @Now);
                SELECT last_insert_rowid();",
                new { DayId = dayId, StepName = stepName, PromptText = promptText, Now = now });
        }

        public void UpdateAiStepLogResult(int logId, string rawOutput, string parsedJson, string status)
        {
            using var conn = GetConnection();
            conn.Execute(@"
                UPDATE ms_ai_step_log 
                SET raw_output = @RawOutput, 
                    parsed_json = @ParsedJson, 
                    status = @Status,
                    finished_at = @FinishedAt
                WHERE id = @Id",
                new
                {
                    Id = logId,
                    RawOutput = rawOutput,
                    ParsedJson = parsedJson,
                    Status = status,
                    FinishedAt = DateTime.Now.ToString("o")
                });
        }

        public MsAiStepLog? GetLatestAiStepLog(int dayId, string stepName)
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<MsAiStepLog>(@"
                SELECT id, day_id, step_name, prompt_text, raw_output, parsed_json, status, created_at, finished_at
                FROM ms_ai_step_log 
                WHERE day_id = @DayId AND step_name = @StepName
                ORDER BY created_at DESC LIMIT 1",
                new { DayId = dayId, StepName = stepName });
        }

        public List<MsAiStepLog> GetAiStepLogsByDay(int dayId)
        {
            using var conn = GetConnection();
            return conn.Query<MsAiStepLog>(
                "SELECT id, day_id, step_name, prompt_text, raw_output, parsed_json, status, created_at, finished_at FROM ms_ai_step_log WHERE day_id = @DayId ORDER BY created_at",
                new { DayId = dayId }).ToList();
        }

        // ========== Review ==========

        public MsReview? GetReviewByDay(int dayId)
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<MsReview>(
                "SELECT * FROM ms_review WHERE day_id = @DayId",
                new { DayId = dayId });
        }

        public int CreateOrUpdateReview(int dayId, int totalScore, string subscoresJson,
            string strengths, string weaknesses, string nextDayPlan, string coreLink)
        {
            using var conn = GetConnection();
            var existing = GetReviewByDay(dayId);
            var now = DateTime.Now.ToString("o");

            if (existing != null)
            {
                conn.Execute(@"
                    UPDATE ms_review 
                    SET total_score = @TotalScore,
                        subscores_json = @SubscoresJson,
                        strengths = @Strengths,
                        weaknesses = @Weaknesses,
                        next_day_plan = @NextDayPlan,
                        core_link = @CoreLink
                    WHERE id = @Id",
                    new
                    {
                        Id = existing.Id,
                        TotalScore = totalScore,
                        SubscoresJson = subscoresJson,
                        Strengths = strengths,
                        Weaknesses = weaknesses,
                        NextDayPlan = nextDayPlan,
                        CoreLink = coreLink
                    });
                return existing.Id;
            }
            else
            {
                return conn.QuerySingle<int>(@"
                    INSERT INTO ms_review (day_id, total_score, subscores_json, strengths, weaknesses, next_day_plan, core_link, created_at)
                    VALUES (@DayId, @TotalScore, @SubscoresJson, @Strengths, @Weaknesses, @NextDayPlan, @CoreLink, @Now);
                    SELECT last_insert_rowid();",
                    new
                    {
                        DayId = dayId,
                        TotalScore = totalScore,
                        SubscoresJson = subscoresJson,
                        Strengths = strengths,
                        Weaknesses = weaknesses,
                        NextDayPlan = nextDayPlan,
                        CoreLink = coreLink,
                        Now = now
                    });
            }
        }

        // ========== ExportLog ==========

        public int CreateExportLog(int dayId, string filePath)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("o");
            return conn.QuerySingle<int>(@"
                INSERT INTO ms_export_log (day_id, file_path, created_at)
                VALUES (@DayId, @FilePath, @Now);
                SELECT last_insert_rowid();",
                new { DayId = dayId, FilePath = filePath, Now = now });
        }

        public List<MsExportLog> GetExportLogsByDay(int dayId)
        {
            using var conn = GetConnection();
            return conn.Query<MsExportLog>(
                "SELECT * FROM ms_export_log WHERE day_id = @DayId ORDER BY created_at DESC",
                new { DayId = dayId }).ToList();
        }
    }
}
