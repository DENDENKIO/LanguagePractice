using LanguagePractice.Helpers;
using System.Collections.Generic;

namespace LanguagePractice.Models
{
    public class PracticeSession
    {
        public long Id { get; set; }
        public string PackId { get; set; } = "POET_BASIC_1";
        public string CreatedAt { get; set; } = "";

        // Drill A
        public string DrillA_Memo { get; set; } = ""; // 五感メモ

        // Drill B
        public string DrillB_Metaphors { get; set; } = ""; // 比喩・変容

        // Drill C
        public string DrillC_Draft { get; set; } = ""; // 初稿
        public string DrillC_Core { get; set; } = ""; // 核
        public string DrillC_Revision { get; set; } = ""; // 推敲（Cut/Move/Add）

        // Wrap
        public string Wrap_BestOne { get; set; } = "";
        public string Wrap_Todo { get; set; } = "";

        public int ElapsedSeconds { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
    }
}
