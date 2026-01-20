using System;
using LanguagePractice.Helpers; // Enumを使うため

namespace LanguagePractice.Models
{
    public class RunLog
    {
        public long Id { get; set; }
        public string OperationKind { get; set; } = ""; // Enumを文字列として保存
        public string Status { get; set; } = "";        // Enumを文字列として保存
        public string CreatedAt { get; set; } = "";
        public string? PromptText { get; set; }
        public string? RawOutput { get; set; }
        public string? ErrorCode { get; set; }

        // C#で扱いやすいようにプロパティラッパーを用意
        // (DB保存時は文字列だが、コード内ではEnumで扱いたい場合用)
        public OperationKind GetOperationKindEnum()
        {
            if (Enum.TryParse<OperationKind>(OperationKind, out var result))
                return result;
            return Helpers.OperationKind.TEXT_GEN; // デフォルト
        }

        public RunStatus GetStatusEnum()
        {
            if (Enum.TryParse<RunStatus>(Status, out var result))
                return result;
            return Helpers.RunStatus.FAILED;
        }
    }
}
