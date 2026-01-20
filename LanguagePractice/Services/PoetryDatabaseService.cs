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
    /// PoetryLab専用データベースサービス（poetry_v1.db）
    /// </summary>
    public class PoetryDatabaseService
    {
        private static readonly string DbFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LanguagePractice");
        private static readonly string DbPath = Path.Combine(DbFolder, "poetry_v1.db");
        private readonly string _connectionString;

        public PoetryDatabaseService()
        {
            // フォルダがなければ作成
            if (!Directory.Exists(DbFolder))
            {
                Directory.CreateDirectory(DbFolder);
            }

            _connectionString = $"Data Source={DbPath}";
            InitializeDatabase();
        }

        /// <summary>
        /// DB接続を取得
        /// </summary>
        private SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <summary>
        /// データベース初期化（テーブル作成）
        /// </summary>
        private void InitializeDatabase()
        {
            using var conn = GetConnection();
            conn.Open();

            var schema = @"
                -- 1. プロジェクト
                CREATE TABLE IF NOT EXISTS pl_project (
                    id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    title           TEXT NOT NULL,
                    style_type      TEXT NOT NULL DEFAULT 'KOU',
                    created_at      TEXT NOT NULL,
                    updated_at      TEXT NOT NULL
                );

                -- 2. Run
                CREATE TABLE IF NOT EXISTS pl_run (
                    id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    project_id      INTEGER NOT NULL,
                    route_name      TEXT NOT NULL DEFAULT '標準Run',
                    status          TEXT NOT NULL DEFAULT 'RUNNING',
                    created_at      TEXT NOT NULL,
                    finished_at     TEXT,
                    FOREIGN KEY (project_id) REFERENCES pl_project(id)
                );

                -- 3. AIステップログ
                CREATE TABLE IF NOT EXISTS pl_ai_step_log (
                    id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    run_id          INTEGER NOT NULL,
                    step_index      INTEGER NOT NULL,
                    step_name       TEXT NOT NULL,
                    input_keys      TEXT,
                    prompt_text     TEXT,
                    raw_output      TEXT,
                    parsed_json     TEXT,
                    status          TEXT NOT NULL DEFAULT 'PENDING',
                    created_at      TEXT NOT NULL,
                    finished_at     TEXT,
                    FOREIGN KEY (run_id) REFERENCES pl_run(id)
                );

                -- 4. テキスト成果物
                CREATE TABLE IF NOT EXISTS pl_text_asset (
                    id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    project_id      INTEGER NOT NULL,
                    run_id          INTEGER,
                    step_log_id     INTEGER,
                    asset_type      TEXT NOT NULL,
                    input_keys_used TEXT,
                    body_text       TEXT NOT NULL,
                    created_at      TEXT NOT NULL,
                    FOREIGN KEY (project_id) REFERENCES pl_project(id),
                    FOREIGN KEY (run_id) REFERENCES pl_run(id),
                    FOREIGN KEY (step_log_id) REFERENCES pl_ai_step_log(id)
                );

                -- 5. Issue
                CREATE TABLE IF NOT EXISTS pl_issue (
                    id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    project_id      INTEGER NOT NULL,
                    run_id          INTEGER,
                    step_log_id     INTEGER,
                    target_type     TEXT NOT NULL,
                    target_index    INTEGER,
                    level           TEXT NOT NULL,
                    symptom         TEXT NOT NULL,
                    severity        TEXT NOT NULL DEFAULT 'B',
                    evidence        TEXT,
                    diagnosis       TEXT,
                    fix_type        TEXT,
                    plan_notes      TEXT,
                    status          TEXT NOT NULL DEFAULT 'OPEN',
                    created_at      TEXT NOT NULL,
                    FOREIGN KEY (project_id) REFERENCES pl_project(id),
                    FOREIGN KEY (run_id) REFERENCES pl_run(id),
                    FOREIGN KEY (step_log_id) REFERENCES pl_ai_step_log(id)
                );

                -- 6. 比較・採択
                CREATE TABLE IF NOT EXISTS pl_compare (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    project_id          INTEGER NOT NULL,
                    run_id              INTEGER NOT NULL,
                    candidate_asset_ids TEXT NOT NULL,
                    winner_asset_id     INTEGER,
                    reason_note         TEXT,
                    created_at          TEXT NOT NULL,
                    FOREIGN KEY (project_id) REFERENCES pl_project(id),
                    FOREIGN KEY (run_id) REFERENCES pl_run(id),
                    FOREIGN KEY (winner_asset_id) REFERENCES pl_text_asset(id)
                );

                -- 7. エクスポートログ
                CREATE TABLE IF NOT EXISTS pl_export_log (
                    id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    project_id      INTEGER NOT NULL,
                    run_id          INTEGER,
                    file_path       TEXT NOT NULL,
                    created_at      TEXT NOT NULL,
                    FOREIGN KEY (project_id) REFERENCES pl_project(id),
                    FOREIGN KEY (run_id) REFERENCES pl_run(id)
                );

                -- インデックス
                CREATE INDEX IF NOT EXISTS idx_pl_run_project ON pl_run(project_id);
                CREATE INDEX IF NOT EXISTS idx_pl_ai_step_log_run ON pl_ai_step_log(run_id);
                CREATE INDEX IF NOT EXISTS idx_pl_text_asset_project ON pl_text_asset(project_id);
                CREATE INDEX IF NOT EXISTS idx_pl_text_asset_run ON pl_text_asset(run_id);
                CREATE INDEX IF NOT EXISTS idx_pl_issue_project ON pl_issue(project_id);
                CREATE INDEX IF NOT EXISTS idx_pl_issue_run ON pl_issue(run_id);
                CREATE INDEX IF NOT EXISTS idx_pl_compare_run ON pl_compare(run_id);
                CREATE INDEX IF NOT EXISTS idx_pl_export_log_project ON pl_export_log(project_id);
            ";

            conn.Execute(schema);
        }

        #region Project CRUD

        public int CreateProject(string title, string styleType)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                INSERT INTO pl_project (title, style_type, created_at, updated_at)
                VALUES (@Title, @StyleType, @Now, @Now);
                SELECT last_insert_rowid();
            ";
            return conn.ExecuteScalar<int>(sql, new { Title = title, StyleType = styleType, Now = now });
        }

        public PlProject? GetProject(int id)
        {
            using var conn = GetConnection();
            var sql = "SELECT id AS Id, title AS Title, style_type AS StyleType, created_at AS CreatedAt, updated_at AS UpdatedAt FROM pl_project WHERE id = @Id";
            return conn.QueryFirstOrDefault<PlProject>(sql, new { Id = id });
        }

        public List<PlProject> GetAllProjects()
        {
            using var conn = GetConnection();
            var sql = "SELECT id AS Id, title AS Title, style_type AS StyleType, created_at AS CreatedAt, updated_at AS UpdatedAt FROM pl_project ORDER BY updated_at DESC";
            return conn.Query<PlProject>(sql).ToList();
        }

        public void UpdateProject(int id, string title, string styleType)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = "UPDATE pl_project SET title = @Title, style_type = @StyleType, updated_at = @Now WHERE id = @Id";
            conn.Execute(sql, new { Id = id, Title = title, StyleType = styleType, Now = now });
        }

        public void DeleteProject(int id)
        {
            using var conn = GetConnection();
            // 関連データも削除（カスケード）
            conn.Execute("DELETE FROM pl_export_log WHERE project_id = @Id", new { Id = id });
            conn.Execute("DELETE FROM pl_compare WHERE project_id = @Id", new { Id = id });
            conn.Execute("DELETE FROM pl_issue WHERE project_id = @Id", new { Id = id });
            conn.Execute("DELETE FROM pl_text_asset WHERE project_id = @Id", new { Id = id });
            conn.Execute("DELETE FROM pl_ai_step_log WHERE run_id IN (SELECT id FROM pl_run WHERE project_id = @Id)", new { Id = id });
            conn.Execute("DELETE FROM pl_run WHERE project_id = @Id", new { Id = id });
            conn.Execute("DELETE FROM pl_project WHERE id = @Id", new { Id = id });
        }

        #endregion

        #region Run CRUD

        public int CreateRun(int projectId, string routeName = "標準Run")
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                INSERT INTO pl_run (project_id, route_name, status, created_at)
                VALUES (@ProjectId, @RouteName, 'RUNNING', @Now);
                SELECT last_insert_rowid();
            ";
            return conn.ExecuteScalar<int>(sql, new { ProjectId = projectId, RouteName = routeName, Now = now });
        }

        public PlRun? GetRun(int id)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT id AS Id, project_id AS ProjectId, route_name AS RouteName, 
                       status AS Status, created_at AS CreatedAt, finished_at AS FinishedAt 
                FROM pl_run WHERE id = @Id
            ";
            return conn.QueryFirstOrDefault<PlRun>(sql, new { Id = id });
        }

        public List<PlRun> GetRunsByProject(int projectId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT id AS Id, project_id AS ProjectId, route_name AS RouteName, 
                       status AS Status, created_at AS CreatedAt, finished_at AS FinishedAt 
                FROM pl_run WHERE project_id = @ProjectId ORDER BY created_at DESC
            ";
            return conn.Query<PlRun>(sql, new { ProjectId = projectId }).ToList();
        }

        public void UpdateRunStatus(int id, string status)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = "UPDATE pl_run SET status = @Status, finished_at = @Now WHERE id = @Id";
            conn.Execute(sql, new { Id = id, Status = status, Now = now });
        }

        #endregion

        #region AiStepLog CRUD

        public int CreateAiStepLog(int runId, int stepIndex, string stepName, string? inputKeys, string? promptText)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                INSERT INTO pl_ai_step_log (run_id, step_index, step_name, input_keys, prompt_text, status, created_at)
                VALUES (@RunId, @StepIndex, @StepName, @InputKeys, @PromptText, 'PENDING', @Now);
                SELECT last_insert_rowid();
            ";
            return conn.ExecuteScalar<int>(sql, new { RunId = runId, StepIndex = stepIndex, StepName = stepName, InputKeys = inputKeys, PromptText = promptText, Now = now });
        }

        public PlAiStepLog? GetAiStepLog(int id)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT id AS Id, run_id AS RunId, step_index AS StepIndex, step_name AS StepName,
                       input_keys AS InputKeys, prompt_text AS PromptText, raw_output AS RawOutput,
                       parsed_json AS ParsedJson, status AS Status, created_at AS CreatedAt, finished_at AS FinishedAt
                FROM pl_ai_step_log WHERE id = @Id
            ";
            return conn.QueryFirstOrDefault<PlAiStepLog>(sql, new { Id = id });
        }

        public List<PlAiStepLog> GetAiStepLogsByRun(int runId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT id AS Id, run_id AS RunId, step_index AS StepIndex, step_name AS StepName,
                       input_keys AS InputKeys, prompt_text AS PromptText, raw_output AS RawOutput,
                       parsed_json AS ParsedJson, status AS Status, created_at AS CreatedAt, finished_at AS FinishedAt
                FROM pl_ai_step_log WHERE run_id = @RunId ORDER BY step_index
            ";
            return conn.Query<PlAiStepLog>(sql, new { RunId = runId }).ToList();
        }

        public void UpdateAiStepLogStatus(int id, string status)
        {
            using var conn = GetConnection();
            var sql = "UPDATE pl_ai_step_log SET status = @Status WHERE id = @Id";
            conn.Execute(sql, new { Id = id, Status = status });
        }

        public void UpdateAiStepLogResult(int id, string rawOutput, string? parsedJson, string status)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                UPDATE pl_ai_step_log 
                SET raw_output = @RawOutput, parsed_json = @ParsedJson, status = @Status, finished_at = @Now 
                WHERE id = @Id
            ";
            conn.Execute(sql, new { Id = id, RawOutput = rawOutput, ParsedJson = parsedJson, Status = status, Now = now });
        }

        #endregion

        #region TextAsset CRUD

        public int CreateTextAsset(int projectId, int? runId, int? stepLogId, string assetType, string? inputKeysUsed, string bodyText)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                INSERT INTO pl_text_asset (project_id, run_id, step_log_id, asset_type, input_keys_used, body_text, created_at)
                VALUES (@ProjectId, @RunId, @StepLogId, @AssetType, @InputKeysUsed, @BodyText, @Now);
                SELECT last_insert_rowid();
            ";
            return conn.ExecuteScalar<int>(sql, new { ProjectId = projectId, RunId = runId, StepLogId = stepLogId, AssetType = assetType, InputKeysUsed = inputKeysUsed, BodyText = bodyText, Now = now });
        }

        public PlTextAsset? GetTextAsset(int id)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT id AS Id, project_id AS ProjectId, run_id AS RunId, step_log_id AS StepLogId,
                       asset_type AS AssetType, input_keys_used AS InputKeysUsed, body_text AS BodyText, created_at AS CreatedAt
                FROM pl_text_asset WHERE id = @Id
            ";
            return conn.QueryFirstOrDefault<PlTextAsset>(sql, new { Id = id });
        }

        public List<PlTextAsset> GetTextAssetsByRun(int runId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT id AS Id, project_id AS ProjectId, run_id AS RunId, step_log_id AS StepLogId,
                       asset_type AS AssetType, input_keys_used AS InputKeysUsed, body_text AS BodyText, created_at AS CreatedAt
                FROM pl_text_asset WHERE run_id = @RunId ORDER BY created_at
            ";
            return conn.Query<PlTextAsset>(sql, new { RunId = runId }).ToList();
        }

        public PlTextAsset? GetTextAssetByType(int runId, string assetType)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT id AS Id, project_id AS ProjectId, run_id AS RunId, step_log_id AS StepLogId,
                       asset_type AS AssetType, input_keys_used AS InputKeysUsed, body_text AS BodyText, created_at AS CreatedAt
                FROM pl_text_asset WHERE run_id = @RunId AND asset_type = @AssetType
                ORDER BY created_at DESC LIMIT 1
            ";
            return conn.QueryFirstOrDefault<PlTextAsset>(sql, new { RunId = runId, AssetType = assetType });
        }

        #endregion

        #region Issue CRUD

        public int CreateIssue(PlIssue issue)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                INSERT INTO pl_issue (project_id, run_id, step_log_id, target_type, target_index, level, symptom, severity, evidence, diagnosis, fix_type, plan_notes, status, created_at)
                VALUES (@ProjectId, @RunId, @StepLogId, @TargetType, @TargetIndex, @Level, @Symptom, @Severity, @Evidence, @Diagnosis, @FixType, @PlanNotes, @Status, @Now);
                SELECT last_insert_rowid();
            ";
            return conn.ExecuteScalar<int>(sql, new
            {
                issue.ProjectId,
                issue.RunId,
                issue.StepLogId,
                issue.TargetType,
                issue.TargetIndex,
                issue.Level,
                issue.Symptom,
                issue.Severity,
                issue.Evidence,
                issue.Diagnosis,
                issue.FixType,
                issue.PlanNotes,
                issue.Status,
                Now = now
            });
        }

        public List<PlIssue> GetIssuesByRun(int runId, string[]? severities = null)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT id AS Id, project_id AS ProjectId, run_id AS RunId, step_log_id AS StepLogId,
                       target_type AS TargetType, target_index AS TargetIndex, level AS Level, symptom AS Symptom,
                       severity AS Severity, evidence AS Evidence, diagnosis AS Diagnosis, fix_type AS FixType,
                       plan_notes AS PlanNotes, status AS Status, created_at AS CreatedAt
                FROM pl_issue WHERE run_id = @RunId
            ";
            if (severities != null && severities.Length > 0)
            {
                sql += " AND severity IN @Severities";
            }
            sql += " ORDER BY CASE severity WHEN 'S' THEN 1 WHEN 'A' THEN 2 WHEN 'B' THEN 3 WHEN 'C' THEN 4 ELSE 5 END";
            return conn.Query<PlIssue>(sql, new { RunId = runId, Severities = severities }).ToList();
        }

        public void UpdateIssueDiagnosis(int id, string diagnosis, string fixType, string planNotes)
        {
            using var conn = GetConnection();
            var sql = "UPDATE pl_issue SET diagnosis = @Diagnosis, fix_type = @FixType, plan_notes = @PlanNotes, status = 'PLANNED' WHERE id = @Id";
            conn.Execute(sql, new { Id = id, Diagnosis = diagnosis, FixType = fixType, PlanNotes = planNotes });
        }

        #endregion

        #region Compare CRUD

        public int CreateCompare(int projectId, int runId, string candidateAssetIds, int? winnerAssetId, string? reasonNote)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                INSERT INTO pl_compare (project_id, run_id, candidate_asset_ids, winner_asset_id, reason_note, created_at)
                VALUES (@ProjectId, @RunId, @CandidateAssetIds, @WinnerAssetId, @ReasonNote, @Now);
                SELECT last_insert_rowid();
            ";
            return conn.ExecuteScalar<int>(sql, new { ProjectId = projectId, RunId = runId, CandidateAssetIds = candidateAssetIds, WinnerAssetId = winnerAssetId, ReasonNote = reasonNote, Now = now });
        }

        public PlCompare? GetCompareByRun(int runId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT id AS Id, project_id AS ProjectId, run_id AS RunId, candidate_asset_ids AS CandidateAssetIds,
                       winner_asset_id AS WinnerAssetId, reason_note AS ReasonNote, created_at AS CreatedAt
                FROM pl_compare WHERE run_id = @RunId ORDER BY created_at DESC LIMIT 1
            ";
            return conn.QueryFirstOrDefault<PlCompare>(sql, new { RunId = runId });
        }

        #endregion

        #region ExportLog CRUD

        public int CreateExportLog(int projectId, int? runId, string filePath)
        {
            using var conn = GetConnection();
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var sql = @"
                INSERT INTO pl_export_log (project_id, run_id, file_path, created_at)
                VALUES (@ProjectId, @RunId, @FilePath, @Now);
                SELECT last_insert_rowid();
            ";
            return conn.ExecuteScalar<int>(sql, new { ProjectId = projectId, RunId = runId, FilePath = filePath, Now = now });
        }

        public List<PlExportLog> GetExportLogsByProject(int projectId)
        {
            using var conn = GetConnection();
            var sql = @"
                SELECT id AS Id, project_id AS ProjectId, run_id AS RunId, file_path AS FilePath, created_at AS CreatedAt
                FROM pl_export_log WHERE project_id = @ProjectId ORDER BY created_at DESC
            ";
            return conn.Query<PlExportLog>(sql, new { ProjectId = projectId }).ToList();
        }

        #endregion
    }
}
