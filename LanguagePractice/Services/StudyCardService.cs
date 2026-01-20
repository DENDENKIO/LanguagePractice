using Dapper;
using LanguagePractice.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace LanguagePractice.Services
{
    public class StudyCardService
    {
        public long CreateStudyCard(StudyCard card)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = @"
                INSERT INTO study_card (source_work_id, created_at, focus, level, best_expressions_raw, metaphor_chains_raw, do_next_raw, tags, full_parsed_content)
                VALUES (@SourceWorkId, @CreatedAt, @Focus, @Level, @BestExpressionsRaw, @MetaphorChainsRaw, @DoNextRaw, @Tags, @FullParsedContent);
                SELECT last_insert_rowid();";
            return conn.QuerySingle<long>(sql, card);
        }

        public void DeleteStudyCard(long id)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Execute("DELETE FROM study_card WHERE id = @Id", new { Id = id });
        }

        public List<StudyCard> SearchStudyCards(string keyword)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Open();

            string norm = FtsSearchHelper.NormalizeQuery(keyword);
            if (norm.Length == 0) return new List<StudyCard>();

            var terms = FtsSearchHelper.SplitAtForAnd(norm);

            bool useLike = terms.Any(t => t.Length < 3) || !FtsSearchHelper.HasFtsTable(conn, "study_card_fts");
            if (useLike)
            {
                string like = $"%{norm}%";
                string likeSql = @"
SELECT * FROM study_card
WHERE
  lower(COALESCE(created_at,'')) LIKE @Key OR
  lower(COALESCE(focus,'')) LIKE @Key OR
  lower(COALESCE(level,'')) LIKE @Key OR
  lower(COALESCE(tags,'')) LIKE @Key OR
  lower(COALESCE(full_parsed_content,'')) LIKE @Key OR
  lower(COALESCE(best_expressions_raw,'')) LIKE @Key OR
  lower(COALESCE(metaphor_chains_raw,'')) LIKE @Key OR
  lower(COALESCE(do_next_raw,'')) LIKE @Key
ORDER BY id DESC LIMIT 100;";
                return conn.Query<StudyCard>(likeSql, new { Key = like }).ToList();
            }

            string q = FtsSearchHelper.BuildAndMatchQuery(terms);

            string ftsSql = @"
SELECT c.*
FROM study_card_fts
JOIN study_card c ON c.id = study_card_fts.rowid
WHERE study_card_fts MATCH @Q
ORDER BY bm25(study_card_fts), c.id DESC
LIMIT 100;";

            return conn.Query<StudyCard>(ftsSql, new { Q = q }).ToList();
        }
    }
}
