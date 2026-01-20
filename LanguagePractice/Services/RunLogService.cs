using Dapper;
using LanguagePractice.Models;
using System.Collections.Generic;
using System.Linq;

namespace LanguagePractice.Services
{
    public class RunLogService
    {
        // ログを新規作成（IDが返る）
        public long CreateLog(RunLog log)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = @"
                INSERT INTO run_log (operation_kind, status, created_at, prompt_text, raw_output, error_code)
                VALUES (@OperationKind, @Status, @CreatedAt, @PromptText, @RawOutput, @ErrorCode);
                SELECT last_insert_rowid();";

            return conn.QuerySingle<long>(sql, log);
        }

        // ログを更新
        public void UpdateLog(RunLog log)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = @"
                UPDATE run_log 
                SET status = @Status, 
                    raw_output = @RawOutput, 
                    error_code = @ErrorCode
                WHERE id = @Id";
            conn.Execute(sql, log);
        }

        // 最新のログを取得
        public List<RunLog> GetRecentLogs(int limit = 50)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = "SELECT * FROM run_log ORDER BY id DESC LIMIT @Limit";
            return conn.Query<RunLog>(sql, new { Limit = limit }).ToList();
        }
    }
}
