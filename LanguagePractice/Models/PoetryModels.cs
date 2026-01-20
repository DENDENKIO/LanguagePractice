namespace LanguagePractice.Models
{
    /// <summary>
    /// PoetryLab プロジェクト
    /// </summary>
    public class PlProject
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string StyleType { get; set; } = "KOU"; // KOU/BU/MIX
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Run（パイプライン実行単位）
    /// </summary>
    public class PlRun
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string RouteName { get; set; } = "標準Run";
        public string Status { get; set; } = "RUNNING"; // RUNNING/SUCCESS/FAILED/CANCELLED
        public string CreatedAt { get; set; } = string.Empty;
        public string? FinishedAt { get; set; }
    }

    /// <summary>
    /// AIステップログ
    /// </summary>
    public class PlAiStepLog
    {
        public int Id { get; set; }
        public int RunId { get; set; }
        public int StepIndex { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string? InputKeys { get; set; } // JSON配列
        public string? PromptText { get; set; }
        public string? RawOutput { get; set; }
        public string? ParsedJson { get; set; }
        public string Status { get; set; } = "PENDING"; // PENDING/RUNNING/SUCCESS/FAILED/SKIPPED
        public string CreatedAt { get; set; } = string.Empty;
        public string? FinishedAt { get; set; }
    }

    /// <summary>
    /// テキスト成果物
    /// </summary>
    public class PlTextAsset
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? RunId { get; set; }
        public int? StepLogId { get; set; }
        public string AssetType { get; set; } = string.Empty; // TOPIC/DRAFT/CORE/REV_A/REV_B/REV_C/WINNER等
        public string? InputKeysUsed { get; set; } // JSON配列
        public string BodyText { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Issue（検出・診断）
    /// </summary>
    public class PlIssue
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? RunId { get; set; }
        public int? StepLogId { get; set; }
        public string TargetType { get; set; } = "ALL"; // LINE/STANZA/ALL
        public int? TargetIndex { get; set; }
        public string Level { get; set; } = string.Empty; // CORE/STRUCTURE/VOICE/IMAGE/READER_EFFECT/SOUND/SURFACE
        public string Symptom { get; set; } = string.Empty;
        public string Severity { get; set; } = "B"; // S/A/B/C
        public string? Evidence { get; set; }
        public string? Diagnosis { get; set; }
        public string? FixType { get; set; } // CUT/MOVE/ADD/REPLACE等
        public string? PlanNotes { get; set; }
        public string Status { get; set; } = "OPEN"; // OPEN/PLANNED/DONE
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// 比較・採択記録
    /// </summary>
    public class PlCompare
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int RunId { get; set; }
        public string CandidateAssetIds { get; set; } = string.Empty; // JSON配列
        public int? WinnerAssetId { get; set; }
        public string? ReasonNote { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// エクスポートログ
    /// </summary>
    public class PlExportLog
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int? RunId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }
}
