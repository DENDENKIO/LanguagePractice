using Dapper;
using LanguagePractice.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace LanguagePractice.Services
{
    public class WorkService
    {
        public long CreateWork(Work work)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = @"
                INSERT INTO work (kind, title, body_text, created_at, run_log_id, writer_name, reader_note, tone_label)
                VALUES (@Kind, @Title, @BodyText, @CreatedAt, @RunLogId, @WriterName, @ReaderNote, @ToneLabel);
                SELECT last_insert_rowid();";
            return conn.QuerySingle<long>(sql, work);
        }

        public void DeleteWork(long id)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Execute("DELETE FROM work WHERE id = @Id", new { Id = id });
        }

        public List<Work> SearchWorks(string keyword)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Open();

            string norm = FtsSearchHelper.NormalizeQuery(keyword);
            if (norm.Length == 0) return new List<Work>();

            var terms = FtsSearchHelper.SplitAtForAnd(norm);

            // trigram想定：3文字未満はヒットしにくいのでLIKEフォールバック
            bool useLike = terms.Any(t => t.Length < 3) || !FtsSearchHelper.HasFtsTable(conn, "work_fts");
            if (useLike)
            {
                string like = $"%{norm}%";
                // lower() でケース無視
                string likeSql = @"
SELECT * FROM work
WHERE
  lower(COALESCE(kind,'')) LIKE @Key OR
  lower(COALESCE(title,'')) LIKE @Key OR
  lower(COALESCE(body_text,'')) LIKE @Key OR
  lower(COALESCE(created_at,'')) LIKE @Key OR
  lower(COALESCE(writer_name,'')) LIKE @Key OR
  lower(COALESCE(reader_note,'')) LIKE @Key OR
  lower(COALESCE(tone_label,'')) LIKE @Key
ORDER BY id DESC
LIMIT 100;";
                return conn.Query<Work>(likeSql, new { Key = like }).ToList();
            }

            string q = FtsSearchHelper.BuildAndMatchQuery(terms);

            string ftsSql = @"
SELECT w.*
FROM work_fts
JOIN work w ON w.id = work_fts.rowid
WHERE work_fts MATCH @Q
ORDER BY bm25(work_fts), w.id DESC
LIMIT 100;";

            return conn.Query<Work>(ftsSql, new { Q = q }).ToList();
        }

        public List<Work> GetRecentWorks(int limit = 100)
        {
            using var conn = DatabaseService.GetConnection();
            return conn.Query<Work>("SELECT * FROM work ORDER BY id DESC LIMIT @Limit", new { Limit = limit }).ToList();
        }
    }
}
