using Dapper;
using LanguagePractice.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace LanguagePractice.Services
{
    public class ObservationService
    {
        public long CreateObservation(Observation obs)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = @"
                INSERT INTO observation (image_url, motif, visual_raw, sound_raw, metaphors_raw, core_candidates_raw, full_content, created_at)
                VALUES (@ImageUrl, @Motif, @VisualRaw, @SoundRaw, @MetaphorsRaw, @CoreCandidatesRaw, @FullContent, @CreatedAt);
                SELECT last_insert_rowid();";
            return conn.QuerySingle<long>(sql, obs);
        }

        public void DeleteObservation(long id)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Execute("DELETE FROM observation WHERE id = @Id", new { Id = id });
        }

        public List<Observation> SearchObservations(string keyword)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Open();

            string norm = FtsSearchHelper.NormalizeQuery(keyword);
            if (norm.Length == 0) return new List<Observation>();

            var terms = FtsSearchHelper.SplitAtForAnd(norm);

            bool useLike = terms.Any(t => t.Length < 3) || !FtsSearchHelper.HasFtsTable(conn, "observation_fts");
            if (useLike)
            {
                string like = $"%{norm}%";
                string likeSql = @"
SELECT * FROM observation
WHERE
  lower(COALESCE(motif,'')) LIKE @Key OR
  lower(COALESCE(image_url,'')) LIKE @Key OR
  lower(COALESCE(visual_raw,'')) LIKE @Key OR
  lower(COALESCE(sound_raw,'')) LIKE @Key OR
  lower(COALESCE(metaphors_raw,'')) LIKE @Key OR
  lower(COALESCE(core_candidates_raw,'')) LIKE @Key OR
  lower(COALESCE(full_content,'')) LIKE @Key
ORDER BY id DESC LIMIT 100;";
                return conn.Query<Observation>(likeSql, new { Key = like }).ToList();
            }

            string q = FtsSearchHelper.BuildAndMatchQuery(terms);

            string ftsSql = @"
SELECT o.*
FROM observation_fts
JOIN observation o ON o.id = observation_fts.rowid
WHERE observation_fts MATCH @Q
ORDER BY bm25(observation_fts), o.id DESC
LIMIT 100;";

            return conn.Query<Observation>(ftsSql, new { Q = q }).ToList();
        }
    }
}
