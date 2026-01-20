using System.Collections.Generic;

namespace LanguagePractice.Helpers
{
    // ==========================================
    // 付録B：Enum固定リスト
    // ==========================================

    public enum OperationKind
    {
        READER_AUTO_GEN,
        TOPIC_GEN,
        PERSONA_GEN,
        OBSERVE_IMAGE,
        TEXT_GEN,
        GIKO,
        STUDY_CARD,
        CORE_EXTRACT,
        REVISION_FULL,
        PERSONA_VERIFY_ASSIST,
        PRACTICE_SESSION
    }

    public enum RunStatus
    {
        SUCCESS,
        FAILED,
        REPAIRED_SUCCESS,
        REPAIRED_FAILED,
        CANCELLED,
        SKIPPED
    }

    public enum WorkKind
    {
        TEXT_GEN,
        GIKO,
        REVISION,
        USER_PRACTICE,
        STUDY_DERIVED,
        ANALYSIS
    }

    public enum VerificationStatus
    {
        UNVERIFIED,
        PARTIALLY_VERIFIED,
        VERIFIED,
        DISPUTED
    }

    public enum ReadingLevel
    {
        EASY,
        NORMAL,
        LITERARY
    }

    public enum LengthProfile
    {
        STUDY_SHORT,
        PRACTICE_MIDDLE,
        REVISION_LONG
    }

    // ==========================================
    // 定数・マーカー定義（付録A・H・K関連）
    // ==========================================
    public static class LpConstants
    {
        // 終端文字列
        public const string DONE_SENTINEL = "⟦LP_DONE_9F3A2C⟧";

        // マーカー定義（開始）
        public static readonly Dictionary<OperationKind, string> MarkerBegin = new Dictionary<OperationKind, string>
        {
            { OperationKind.READER_AUTO_GEN, "<<<READER_BEGIN>>>" },
            { OperationKind.TOPIC_GEN, "<<<TOPIC_PACK_BEGIN>>>" },
            { OperationKind.PERSONA_GEN, "<<<PERSONA_PACK_BEGIN>>>" },
            { OperationKind.OBSERVE_IMAGE, "<<<OBSERVATION_BEGIN>>>" },
            { OperationKind.TEXT_GEN, "<<<TEXT_GEN_BEGIN>>>" },
            { OperationKind.GIKO, "<<<GIKO_BEGIN>>>" },
            { OperationKind.STUDY_CARD, "<<<STUDY_CARD_BEGIN>>>" },
            { OperationKind.CORE_EXTRACT, "<<<CORE_BEGIN>>>" },
            { OperationKind.REVISION_FULL, "<<<REVISION_PACK_BEGIN>>>" },
            { OperationKind.PERSONA_VERIFY_ASSIST, "<<<PERSONA_VERIFY_BEGIN>>>" }
        };

        // マーカー定義（終了）
        public static readonly Dictionary<OperationKind, string> MarkerEnd = new Dictionary<OperationKind, string>
        {
            { OperationKind.READER_AUTO_GEN, "<<<READER_END>>>" },
            { OperationKind.TOPIC_GEN, "<<<TOPIC_PACK_END>>>" },
            { OperationKind.PERSONA_GEN, "<<<PERSONA_PACK_END>>>" },
            { OperationKind.OBSERVE_IMAGE, "<<<OBSERVATION_END>>>" },
            { OperationKind.TEXT_GEN, "<<<TEXT_GEN_END>>>" },
            { OperationKind.GIKO, "<<<GIKO_END>>>" },
            { OperationKind.STUDY_CARD, "<<<STUDY_CARD_END>>>" },
            { OperationKind.CORE_EXTRACT, "<<<CORE_END>>>" },
            { OperationKind.REVISION_FULL, "<<<REVISION_PACK_END>>>" },
            { OperationKind.PERSONA_VERIFY_ASSIST, "<<<PERSONA_VERIFY_END>>>" }
        };
    }
}
