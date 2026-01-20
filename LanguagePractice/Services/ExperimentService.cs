using Dapper;
using LanguagePractice.Models;
using System.Collections.Generic;
using System.Linq;

namespace LanguagePractice.Services
{
    public class ExperimentService
    {
        public long CreateExperiment(Experiment exp, List<ExperimentTrial> trials)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                string sqlExp = @"
                    INSERT INTO experiment (title, description, created_at, variable_name, common_topic, common_writer)
                    VALUES (@Title, @Description, @CreatedAt, @VariableName, @CommonTopic, @CommonWriter);
                    SELECT last_insert_rowid();";
                long expId = conn.QuerySingle<long>(sqlExp, exp, trans);

                string sqlTrial = @"
                    INSERT INTO experiment_trial (experiment_id, variable_value, result_work_id, rating)
                    VALUES (@ExperimentId, @VariableValue, @ResultWorkId, @Rating);";

                foreach (var t in trials)
                {
                    t.ExperimentId = expId;
                    conn.Execute(sqlTrial, t, trans);
                }

                trans.Commit();
                return expId;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public List<Experiment> GetAllExperiments()
        {
            using var conn = DatabaseService.GetConnection();
            return conn.Query<Experiment>("SELECT * FROM experiment ORDER BY id DESC").ToList();
        }

        public List<ExperimentTrial> GetTrials(long expId)
        {
            using var conn = DatabaseService.GetConnection();
            return conn.Query<ExperimentTrial>("SELECT * FROM experiment_trial WHERE experiment_id = @Id", new { Id = expId }).ToList();
        }

        public void UpdateTrial(ExperimentTrial trial)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Execute("UPDATE experiment_trial SET result_work_id = @ResultWorkId, rating = @Rating WHERE id = @Id", trial);
        }
    }
}
