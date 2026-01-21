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
    /// MindsetLab専用データベースサービス（mindset_v1.db）
    /// 仕様書2 第3.2章 / 第6章準拠
    /// </summary>
    public class MindsetDatabaseService
    {
        private static readonly string DbFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LanguagePractice");
        private static readonly string DbPath = Path.Combine(DbFolder, "mindset_v1.db");
        private readonly string _connectionString;

        public MindsetDatabaseService()
        {
            if (!Directory.Exists(DbFolder))
            {
                Directory.CreateDirectory(DbFolder);
            }

            _connectionString = $"Data Source={DbPath}";
            InitializeDatabase();
        }

        private SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <summary>
        /// データベース初期化（仕様書2 第6.1章準拠）
        /// </summary>
        private void InitializeDatabase()
        {
            using var conn = GetConnection();
            conn.Open();

            var schema = @"
                -- 1. プロファイル
                CREATE TABLE IF NOT EXISTS ms_profile (
                    id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    name        TEXT,
                    created_at  TEXT NOT NULL
                );

                -- 2. 日単位セッション
                CREATE TABLE IF NOT EXISTS ms_day (
                    id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    date_key        TEXT NOT NULL UNIQUE,
                    focus_mindsets  TEXT,
                    start_ritual    TEXT,
                    end_ritual      TEXT,
                    created_at      TEXT NOT NULL,
                    updated_at      TEXT NOT NULL
                );

                -- 3. ドリル入力
                CREATE TABLE IF NOT EXISTS ms_entry (
                    id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    day_id      INTEGER NOT NULL,
                    entry_type  TEXT NOT NULL,
                    body_text   TEXT,
                    created_at  TEXT NOT NULL,
                    FOREIGN KEY (day_id) REFERENCES ms_day(id)
                );

                -- 4. AIステップログ
                CREATE TABLE IF NOT EXISTS ms_ai_step_log (
                    id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    day_id      INTEGER NOT NULL,
                    step_name   TEXT NOT NULL,
                    prompt_text TEXT,
                    raw_output  TEXT,
                    parsed_json TEXT,
                    status      TEXT NOT NULL DEFAULT 'PENDING',
                    created_at  TEXT NOT NULL,
                    FOREIGN KEY (day_id) REFERENCES ms_day(id)
                );

                -- 5. レビュー
                CREATE TABLE IF NOT EXISTS ms_review (
                    id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    day_id          INTEGER NOT NULL UNIQUE,
                    total_score     INTEGER,
                    subscores_json  TEXT,
                    strengths       TEXT,
                    weaknesses      TEXT,
                    next_day_plan   TEXT,
                    core_link       TEXT,
                    created_at      TEXT NOT NULL,
                    FOREIGN KEY (day_id) REFERENCES ms_day(id)
                );

                -- 6. エクスポートログ
                CREATE TABLE IF NOT EXISTS ms_export_log (
                    id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    day_id      INTEGER NOT NULL,
                    file_path   TEXT NOT NULL,
                    created_at  TEXT NOT NULL,
                    FOREIGN KEY (day_id) REFERENCES ms_day(id)
                );

                -- インデックス
                CREATE INDEX IF NOT EXISTS idx_ms_day_date ON ms_day(date_key);
                CREATE INDEX IF NOT EXISTS idx_ms_entry_day ON ms_entry(day_id);
                CREATE INDEX IF NOT EXISTS idx_ms_ai_step_log_day ON ms_ai_step_log(day_id);
            ";

            conn.Execute(schema);
        }

        #region MsDay CRUD

        public int CreateDay(string dateKey, string focusMindsets = "", string startRitual = "", string endRitual = "")
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                INSERT INTO ms_day (date_key, focus_mindsets, start_ritual, end_ritual, created_at, updated_at)
                VALUES (@DateKey, @FocusMindsets, @StartRitual, @EndRitual, @Now, @Now);
                SELECT last_insert_rowid();
            ";
            return conn.ExecuteScalar<int>(sql, new { DateKey = dateKey, FocusMindsets = focusMindsets, StartRitual = startRitual, EndRitual = endRitual, Now = now });
        }

        public MsDay? GetDayByDate(string dateKey)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM ms_day WHERE date_key = @DateKey";
            return conn.QueryFirstOrDefault<MsDay>(sql, new { DateKey = dateKey });
        }

        public MsDay? GetDay(int id)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM ms_day WHERE id = @Id";
            return conn.QueryFirstOrDefault<MsDay>(sql, new { Id = id });
        }

        public List<MsDay> GetRecentDays(int limit = 30)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM ms_day ORDER BY date_key DESC LIMIT @Limit";
            return conn.Query<MsDay>(sql, new { Limit = limit }).ToList();
        }

        public void UpdateDay(int id, string focusMindsets, string startRitual, string endRitual)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = "UPDATE ms_day SET focus_mindsets = @FocusMindsets, start_ritual = @StartRitual, end_ritual = @EndRitual, updated_at = @Now WHERE id = @Id";
            conn.Execute(sql, new { Id = id, FocusMindsets = focusMindsets, StartRitual = startRitual, EndRitual = endRitual, Now = now });
        }

        public MsDay GetOrCreateToday()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var existing = GetDayByDate(today);
            if (existing != null) return existing;

            var id = CreateDay(today);
            return GetDay(id)!;
        }

        #endregion

        #region MsEntry CRUD

        public int CreateEntry(int dayId, string entryType, string bodyText)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                INSERT INTO ms_entry (day_id, entry_type, body_text, created_at)
                VALUES (@DayId, @EntryType, @BodyText, @Now);
                SELECT last_insert_rowid();
            ";
            return conn.ExecuteScalar<int>(sql, new { DayId = dayId, EntryType = entryType, BodyText = bodyText, Now = now });
        }

        public void UpsertEntry(int dayId, string entryType, string bodyText)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            // 既存があれば更新、なければ挿入
            var existing = conn.QueryFirstOrDefault<MsEntry>(
                "SELECT * FROM ms_entry WHERE day_id = @DayId AND entry_type = @EntryType",
                new { DayId = dayId, EntryType = entryType });

            if (existing != null)
            {
                conn.Execute(
                    "UPDATE ms_entry SET body_text = @BodyText WHERE id = @Id",
                    new { Id = existing.Id, BodyText = bodyText });
            }
            else
            {
                conn.Execute(
                    "INSERT INTO ms_entry (day_id, entry_type, body_text, created_at) VALUES (@DayId, @EntryType, @BodyText, @Now)",
                    new { DayId = dayId, EntryType = entryType, BodyText = bodyText, Now = now });
            }
        }

        public List<MsEntry> GetEntriesByDay(int dayId)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM ms_entry WHERE day_id = @DayId ORDER BY entry_type";
            return conn.Query<MsEntry>(sql, new { DayId = dayId }).ToList();
        }

        public MsEntry? GetEntry(int dayId, string entryType)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM ms_entry WHERE day_id = @DayId AND entry_type = @EntryType";
            return conn.QueryFirstOrDefault<MsEntry>(sql, new { DayId = dayId, EntryType = entryType });
        }

        #endregion

        #region MsAiStepLog CRUD

        public int CreateAiStepLog(int dayId, string stepName, string promptText)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                INSERT INTO ms_ai_step_log (day_id, step_name, prompt_text, status, created_at)
                VALUES (@DayId, @StepName, @PromptText, 'PENDING', @Now);
                SELECT last_insert_rowid();
            ";
            return conn.ExecuteScalar<int>(sql, new { DayId = dayId, StepName = stepName, PromptText = promptText, Now = now });
        }

        public void UpdateAiStepLogResult(int id, string rawOutput, string parsedJson, string status)
        {
            using var conn = GetConnection();
            var sql = "UPDATE ms_ai_step_log SET raw_output = @RawOutput, parsed_json = @ParsedJson, status = @Status WHERE id = @Id";
            conn.Execute(sql, new { Id = id, RawOutput = rawOutput, ParsedJson = parsedJson, Status = status });
        }

        public List<MsAiStepLog> GetAiStepLogsByDay(int dayId)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM ms_ai_step_log WHERE day_id = @DayId ORDER BY created_at";
            return conn.Query<MsAiStepLog>(sql, new { DayId = dayId }).ToList();
        }

        public MsAiStepLog? GetLatestAiStepLog(int dayId, string stepName)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM ms_ai_step_log WHERE day_id = @DayId AND step_name = @StepName ORDER BY created_at DESC LIMIT 1";
            return conn.QueryFirstOrDefault<MsAiStepLog>(sql, new { DayId = dayId, StepName = stepName });
        }

        #endregion

        #region MsReview CRUD

        public int CreateOrUpdateReview(int dayId, int totalScore, string subscoresJson, string strengths, string weaknesses, string nextDayPlan, string coreLink)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            var existing = conn.QueryFirstOrDefault<MsReview>(
                "SELECT * FROM ms_review WHERE day_id = @DayId",
                new { DayId = dayId });

            if (existing != null)
            {
                conn.Execute(@"
                    UPDATE ms_review SET 
                        total_score = @TotalScore, subscores_json = @SubscoresJson, 
                        strengths = @Strengths, weaknesses = @Weaknesses, 
                        next_day_plan = @NextDayPlan, core_link = @CoreLink
                    WHERE id = @Id",
                    new { Id = existing.Id, TotalScore = totalScore, SubscoresJson = subscoresJson, Strengths = strengths, Weaknesses = weaknesses, NextDayPlan = nextDayPlan, CoreLink = coreLink });
                return existing.Id;
            }
            else
            {
                var sql = @"
                    INSERT INTO ms_review (day_id, total_score, subscores_json, strengths, weaknesses, next_day_plan, core_link, created_at)
                    VALUES (@DayId, @TotalScore, @SubscoresJson, @Strengths, @Weaknesses, @NextDayPlan, @CoreLink, @Now);
                    SELECT last_insert_rowid();
                ";
                return conn.ExecuteScalar<int>(sql, new { DayId = dayId, TotalScore = totalScore, SubscoresJson = subscoresJson, Strengths = strengths, Weaknesses = weaknesses, NextDayPlan = nextDayPlan, CoreLink = coreLink, Now = now });
            }
        }

        public MsReview? GetReviewByDay(int dayId)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM ms_review WHERE day_id = @DayId";
            return conn.QueryFirstOrDefault<MsReview>(sql, new { DayId = dayId });
        }

        #endregion

        #region MsExportLog CRUD

        public int CreateExportLog(int dayId, string filePath)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                INSERT INTO ms_export_log (day_id, file_path, created_at)
                VALUES (@DayId, @FilePath, @Now);
                SELECT last_insert_rowid();
            ";
            return conn.ExecuteScalar<int>(sql, new { DayId = dayId, FilePath = filePath, Now = now });
        }

        public List<MsExportLog> GetExportLogsByDay(int dayId)
        {
            using var conn = GetConnection();
            var sql = "SELECT * FROM ms_export_log WHERE day_id = @DayId ORDER BY created_at DESC";
            return conn.Query<MsExportLog>(sql, new { DayId = dayId }).ToList();
        }

        #endregion

        #region 統計

        public int GetConsecutiveDays()
        {
            using var conn = GetConnection();
            var days = conn.Query<string>("SELECT date_key FROM ms_day ORDER BY date_key DESC").ToList();
            if (days.Count == 0) return 0;

            int count = 0;
            var current = DateTime.Now.Date;

            foreach (var dk in days)
            {
                if (DateTime.TryParse(dk, out var d))
                {
                    if (d.Date == current || d.Date == current.AddDays(-1))
                    {
                        count++;
                        current = d.Date;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return count;
        }

        #endregion
    }
}
