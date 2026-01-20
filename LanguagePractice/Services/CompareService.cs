using Dapper;
using LanguagePractice.Models;
using System.Collections.Generic;

namespace LanguagePractice.Services
{
    public class CompareService
    {
        public void SaveComparison(CompareSet set, List<CompareItem> items)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Set保存
                string sqlSet = @"
                    INSERT INTO compare_set (title, note, winner_work_id, created_at)
                    VALUES (@Title, @Note, @WinnerWorkId, @CreatedAt);
                    SELECT last_insert_rowid();";
                long setId = conn.QuerySingle<long>(sqlSet, set, trans);

                // 2. Items保存
                string sqlItem = @"
                    INSERT INTO compare_item (compare_set_id, work_id, position)
                    VALUES (@CompareSetId, @WorkId, @Position);";

                foreach (var item in items)
                {
                    item.CompareSetId = setId;
                    conn.Execute(sqlItem, item, trans);
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
    }
}
