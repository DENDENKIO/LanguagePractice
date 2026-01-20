using Dapper;
using LanguagePractice.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace LanguagePractice.Services
{
    public class TopicService
    {
        public long CreateTopic(Topic topic)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = @"
                INSERT INTO topic (title, emotion, scene, tags, fix_conditions, created_at)
                VALUES (@Title, @Emotion, @Scene, @Tags, @FixConditions, @CreatedAt);
                SELECT last_insert_rowid();";
            return conn.QuerySingle<long>(sql, topic);
        }

        public void DeleteTopic(long id)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Execute("DELETE FROM topic WHERE id = @Id", new { Id = id });
        }

        public List<Topic> SearchTopics(string keyword)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Open();

            string norm = FtsSearchHelper.NormalizeQuery(keyword);
            if (norm.Length == 0) return new List<Topic>();

            var terms = FtsSearchHelper.SplitAtForAnd(norm);

            bool useLike = terms.Any(t => t.Length < 3) || !FtsSearchHelper.HasFtsTable(conn, "topic_fts");
            if (useLike)
            {
                string like = $"%{norm}%";
                string likeSql = @"
SELECT * FROM topic
WHERE
  lower(COALESCE(title,'')) LIKE @Key OR
  lower(COALESCE(emotion,'')) LIKE @Key OR
  lower(COALESCE(scene,'')) LIKE @Key OR
  lower(COALESCE(tags,'')) LIKE @Key OR
  lower(COALESCE(fix_conditions,'')) LIKE @Key
ORDER BY id DESC LIMIT 100;";
                return conn.Query<Topic>(likeSql, new { Key = like }).ToList();
            }

            string q = FtsSearchHelper.BuildAndMatchQuery(terms);

            string ftsSql = @"
SELECT t.*
FROM topic_fts
JOIN topic t ON t.id = topic_fts.rowid
WHERE topic_fts MATCH @Q
ORDER BY bm25(topic_fts), t.id DESC
LIMIT 100;";

            return conn.Query<Topic>(ftsSql, new { Q = q }).ToList();
        }

        public List<Topic> GetRecentTopics(int limit = 100)
        {
            using var conn = DatabaseService.GetConnection();
            return conn.Query<Topic>("SELECT * FROM topic ORDER BY id DESC LIMIT @Limit", new { Limit = limit }).ToList();
        }
    }
}
