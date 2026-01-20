using Dapper;
using LanguagePractice.Models;
using System.Collections.Generic;
using System.Linq;

namespace LanguagePractice.Services
{
    public class PersonaVerificationService
    {
        public long SaveVerification(PersonaVerification verification)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = @"
                INSERT INTO persona_verification (persona_id, created_at, evidence1, evidence2, evidence3, result_json, overall_verdict, revised_bio_draft)
                VALUES (@PersonaId, @CreatedAt, @Evidence1, @Evidence2, @Evidence3, @ResultJson, @OverallVerdict, @RevisedBioDraft);
                SELECT last_insert_rowid();";
            return conn.QuerySingle<long>(sql, verification);
        }

        public List<PersonaVerification> GetHistory(long personaId)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = "SELECT * FROM persona_verification WHERE persona_id = @Id ORDER BY id DESC";
            return conn.Query<PersonaVerification>(sql, new { Id = personaId }).ToList();
        }
    }
}
