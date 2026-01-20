using Dapper;
using LanguagePractice.Helpers;
using LanguagePractice.Models;
using System.Collections.Generic;

namespace LanguagePractice.Services
{
    public class PracticeService
    {
        // パック情報の定義クラス
        public class PracticePackInfo
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public int TotalMinutes { get; set; }
        }

        // 定義リストを返すメソッド
        public List<PracticePackInfo> GetAvailablePacks()
        {
            return new List<PracticePackInfo>
            {
                new PracticePackInfo
                {
                    Id = "POET_BASIC_1", // Enumではなく文字列でもOK
                    Title = "詩人基礎① (Basic Poet)",
                    Description = "五感メモ→比喩→初稿→推敲のフルコース。",
                    TotalMinutes = 60
                },
                new PracticePackInfo
                {
                    Id = "REVISION_FOCUS",
                    Title = "推敲特化 (Revision Focus)",
                    Description = "既存の文章を削り、動かし、足すことに集中する短縮版。",
                    TotalMinutes = 30
                },
                new PracticePackInfo
                {
                    Id = "SOUND_RHYTHM",
                    Title = "音調トレーニング (Sound & Rhythm)",
                    Description = "朗読を前提とした、リズムと響きの調整練習。",
                    TotalMinutes = 20
                }
            };
        }

        public void CreateTableIfNotExists()
        {
            using var conn = DatabaseService.GetConnection();
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS practice_session (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    pack_id TEXT,
                    created_at TEXT,
                    drill_a_memo TEXT,
                    drill_b_metaphors TEXT,
                    drill_c_draft TEXT,
                    drill_c_core TEXT,
                    drill_c_revision TEXT,
                    wrap_best_one TEXT,
                    wrap_todo TEXT,
                    elapsed_seconds INTEGER,
                    is_completed INTEGER
                );
            ");
        }

        public long CreateSession(PracticeSession session)
        {
            CreateTableIfNotExists();

            using var conn = DatabaseService.GetConnection();
            string sql = @"
                INSERT INTO practice_session (pack_id, created_at, drill_a_memo, drill_b_metaphors, drill_c_draft, drill_c_core, drill_c_revision, wrap_best_one, wrap_todo, elapsed_seconds, is_completed)
                VALUES (@PackId, @CreatedAt, @DrillA_Memo, @DrillB_Metaphors, @DrillC_Draft, @DrillC_Core, @DrillC_Revision, @Wrap_BestOne, @Wrap_Todo, @ElapsedSeconds, @IsCompleted);
                SELECT last_insert_rowid();";

            return conn.QuerySingle<long>(sql, session);
        }

        public void UpdateSession(PracticeSession session)
        {
            using var conn = DatabaseService.GetConnection();
            string sql = @"
                UPDATE practice_session
                SET drill_a_memo = @DrillA_Memo,
                    drill_b_metaphors = @DrillB_Metaphors,
                    drill_c_draft = @DrillC_Draft,
                    drill_c_core = @DrillC_Core,
                    drill_c_revision = @DrillC_Revision,
                    wrap_best_one = @Wrap_BestOne,
                    wrap_todo = @Wrap_Todo,
                    elapsed_seconds = @ElapsedSeconds,
                    is_completed = @IsCompleted
                WHERE id = @Id";

            conn.Execute(sql, session);
        }
    }
}
