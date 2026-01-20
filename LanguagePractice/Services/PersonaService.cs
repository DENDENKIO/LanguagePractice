using Dapper;
using LanguagePractice.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace LanguagePractice.Services
{
    public class PersonaService
    {
        public long CreatePersona(Persona persona)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = @"
                INSERT INTO persona (name, location, bio, style, tags, verification_status, created_at)
                VALUES (@Name, @Location, @Bio, @Style, @Tags, @VerificationStatus, @CreatedAt);
                SELECT last_insert_rowid();";
            return conn.QuerySingle<long>(sql, persona);
        }

        public List<Persona> GetAllPersonas()
        {
            using var conn = DatabaseService.GetConnection();
            return conn.Query<Persona>("SELECT * FROM persona ORDER BY id DESC").ToList();
        }

        public void UpdateStatus(long id, string status)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Execute("UPDATE persona SET verification_status = @Status WHERE id = @Id", new { Id = id, Status = status });
        }

        public void DeletePersona(long id)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Execute("DELETE FROM persona WHERE id = @Id", new { Id = id });
        }

        public List<Persona> SearchPersonas(string keyword)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Open();

            string norm = FtsSearchHelper.NormalizeQuery(keyword);
            if (norm.Length == 0) return new List<Persona>();

            var terms = FtsSearchHelper.SplitAtForAnd(norm);

            bool useLike = terms.Any(t => t.Length < 3) || !FtsSearchHelper.HasFtsTable(conn, "persona_fts");
            if (useLike)
            {
                string like = $"%{norm}%";
                string likeSql = @"
SELECT * FROM persona
WHERE
  lower(COALESCE(name,'')) LIKE @Key OR
  lower(COALESCE(location,'')) LIKE @Key OR
  lower(COALESCE(bio,'')) LIKE @Key OR
  lower(COALESCE(style,'')) LIKE @Key OR
  lower(COALESCE(tags,'')) LIKE @Key OR
  lower(COALESCE(verification_status,'')) LIKE @Key
ORDER BY id DESC LIMIT 100;";
                return conn.Query<Persona>(likeSql, new { Key = like }).ToList();
            }

            string q = FtsSearchHelper.BuildAndMatchQuery(terms);

            string ftsSql = @"
SELECT p.*
FROM persona_fts
JOIN persona p ON p.id = persona_fts.rowid
WHERE persona_fts MATCH @Q
ORDER BY bm25(persona_fts), p.id DESC
LIMIT 100;";

            return conn.Query<Persona>(ftsSql, new { Q = q }).ToList();
        }
    }
}
