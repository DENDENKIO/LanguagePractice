using System.Collections.Generic;

namespace LanguagePractice.Services
{
    /// <summary>
    /// 標準Runのステップ定義
    /// </summary>
    public class PoetryStepDefinition
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string[] InputKeys { get; set; } = System.Array.Empty<string>();
        public string[] OutputKeys { get; set; } = System.Array.Empty<string>();
        public bool IsHumanStep { get; set; } = false;
        public bool IsAiStep => !IsHumanStep;

        /// <summary>
        /// 標準Runのステップ一覧
        /// </summary>
        public static List<PoetryStepDefinition> StandardRun => new()
        {
            new PoetryStepDefinition
            {
                Index = 1,
                Name = "POEM_TOPIC_GEN",
                DisplayName = "Topic生成",
                InputKeys = System.Array.Empty<string>(),
                OutputKeys = new[] { "TOPIC" },
                IsHumanStep = false
            },
            new PoetryStepDefinition
            {
                Index = 2,
                Name = "POEM_DRAFT_GEN",
                DisplayName = "Draft生成",
                InputKeys = new[] { "TOPIC" },
                OutputKeys = new[] { "DRAFT" },
                IsHumanStep = false
            },
            new PoetryStepDefinition
            {
                Index = 3,
                Name = "POEM_CORE_EXTRACT",
                DisplayName = "Core抽出",
                InputKeys = new[] { "DRAFT" },
                OutputKeys = new[] { "CORE_CANDIDATES" },
                IsHumanStep = false
            },
            new PoetryStepDefinition
            {
                Index = 4,
                Name = "CORE_ADOPT",
                DisplayName = "Core採択",
                InputKeys = new[] { "CORE_CANDIDATES" },
                OutputKeys = new[] { "CORE" },
                IsHumanStep = true
            },
            new PoetryStepDefinition
            {
                Index = 5,
                Name = "POEM_LINE_MAP",
                DisplayName = "Line Map生成",
                InputKeys = new[] { "DRAFT", "CORE" },
                OutputKeys = new[] { "LINE_MAP" },
                IsHumanStep = false
            },
            new PoetryStepDefinition
            {
                Index = 6,
                Name = "POEM_ISSUE_GEN",
                DisplayName = "Issue生成",
                InputKeys = new[] { "DRAFT", "CORE", "LINE_MAP" },
                OutputKeys = new[] { "ISSUES" },
                IsHumanStep = false
            },
            new PoetryStepDefinition
            {
                Index = 7,
                Name = "POEM_DIAGNOSE_GEN",
                DisplayName = "Diagnosis生成",
                InputKeys = new[] { "ISSUES" },
                OutputKeys = new[] { "DIAGNOSES" },
                IsHumanStep = false
            },
            new PoetryStepDefinition
            {
                Index = 8,
                Name = "POEM_REVISION_GEN",
                DisplayName = "Revision生成",
                InputKeys = new[] { "DRAFT", "CORE", "DIAGNOSES" },
                OutputKeys = new[] { "REV_A", "REV_B", "REV_C" },
                IsHumanStep = false
            },
            new PoetryStepDefinition
            {
                Index = 9,
                Name = "WINNER_SELECT",
                DisplayName = "Winner選択",
                InputKeys = new[] { "REV_A", "REV_B", "REV_C" },
                OutputKeys = new[] { "WINNER" },
                IsHumanStep = true
            },
            new PoetryStepDefinition
            {
                Index = 10,
                Name = "EXPORT",
                DisplayName = "Export",
                InputKeys = new[] { "TOPIC", "DRAFT", "CORE", "LINE_MAP", "ISSUES", "DIAGNOSES", "REV_A", "REV_B", "REV_C", "WINNER" },
                OutputKeys = System.Array.Empty<string>(),
                IsHumanStep = false
            }
        };
    }
}
