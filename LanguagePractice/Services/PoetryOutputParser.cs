using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LanguagePractice.Services
{
    /// <summary>
    /// AI出力のパース結果
    /// </summary>
    public class PoetryParseResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public Dictionary<string, string> Data { get; set; } = new();
        public string RawSection { get; set; } = string.Empty;
    }

    /// <summary>
    /// PoetryLab用AI出力パーサー
    /// </summary>
    public class PoetryOutputParser
    {
        private const string DONE_SENTINEL = "⟦LP_DONE_9F3A2C⟧";

        /// <summary>
        /// AI出力をパース
        /// </summary>
        public PoetryParseResult Parse(string rawOutput, string expectedSection)
        {
            var result = new PoetryParseResult();

            // 正規化
            var normalized = Normalize(rawOutput);

            // 終端センチネル確認
            if (!normalized.Contains(DONE_SENTINEL))
            {
                result.Success = false;
                result.ErrorMessage = "終端センチネルが見つかりません";
                return result;
            }

            // セクション抽出
            var sectionPattern = $@"⟦BEGIN_SECTION:{Regex.Escape(expectedSection)}⟧(.*?)⟦END_SECTION:{Regex.Escape(expectedSection)}⟧";
            var match = Regex.Match(normalized, sectionPattern, RegexOptions.Singleline);

            if (!match.Success)
            {
                result.Success = false;
                result.ErrorMessage = $"セクション {expectedSection} が見つかりません";
                return result;
            }

            result.SectionName = expectedSection;
            result.RawSection = match.Groups[1].Value.Trim();

            // KEY: VALUE 形式をパース
            result.Data = ParseKeyValues(result.RawSection);
            result.Success = true;

            return result;
        }

        /// <summary>
        /// 複数セクションをパース（REVISIONS用）
        /// </summary>
        public PoetryParseResult ParseRevisions(string rawOutput)
        {
            var result = Parse(rawOutput, "REVISIONS");
            if (!result.Success) return result;

            // REV_A_BODY, REV_B_BODY, REV_C_BODY を抽出
            var bodyPattern = @"(REV_[ABC]_BODY):\s*([\s\S]*?)(?=REV_[ABC]_|⟦END_SECTION|$)";
            var matches = Regex.Matches(result.RawSection, bodyPattern);

            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value.Trim();
                result.Data[key] = value;
            }

            return result;
        }

        /// <summary>
        /// 出力を正規化
        /// </summary>
        private string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // CRLF → LF
            var normalized = input.Replace("\r\n", "\n").Replace("\r", "\n");

            // ゼロ幅スペース等の不可視文字を除去
            normalized = Regex.Replace(normalized, @"[\u200B-\u200D\uFEFF]", "");

            // KEY:VALUE → KEY: VALUE（スペース正規化）
            normalized = Regex.Replace(normalized, @"^([A-Z_0-9]+):(?!\s)", "$1: ", RegexOptions.Multiline);

            return normalized.Trim();
        }

        /// <summary>
        /// KEY: VALUE 形式をパース
        /// </summary>
        private Dictionary<string, string> ParseKeyValues(string section)
        {
            var data = new Dictionary<string, string>();
            var lines = section.Split('\n');
            string? currentKey = null;
            var currentValue = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                // KEY: VALUE パターン
                var keyMatch = Regex.Match(line, @"^([A-Z_0-9]+):\s*(.*)$");
                if (keyMatch.Success)
                {
                    // 前のキーを保存
                    if (currentKey != null)
                    {
                        data[currentKey] = currentValue.ToString().Trim();
                    }

                    currentKey = keyMatch.Groups[1].Value;
                    currentValue.Clear();
                    currentValue.AppendLine(keyMatch.Groups[2].Value);
                }
                else if (currentKey != null)
                {
                    // 継続行
                    currentValue.AppendLine(line);
                }
            }

            // 最後のキーを保存
            if (currentKey != null)
            {
                data[currentKey] = currentValue.ToString().Trim();
            }

            return data;
        }

        /// <summary>
        /// ISSUES セクションから PlIssue リストを生成
        /// </summary>
        public List<Models.PlIssue> ParseIssues(string rawOutput, int projectId, int runId, int stepLogId)
        {
            var result = Parse(rawOutput, "ISSUES");
            if (!result.Success) return new List<Models.PlIssue>();

            var issues = new List<Models.PlIssue>();
            var issuePattern = @"ISSUE_(\d+)_TARGET:\s*(.+?)\s*\nISSUE_\1_LEVEL:\s*(.+?)\s*\nISSUE_\1_SYMPTOM:\s*(.+?)\s*\nISSUE_\1_SEVERITY:\s*(.+?)\s*\nISSUE_\1_EVIDENCE:\s*(.+?)(?=\n\nISSUE_|\n*$)";
            var matches = Regex.Matches(result.RawSection, issuePattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var targetParts = match.Groups[2].Value.Trim().Split('/');
                var issue = new Models.PlIssue
                {
                    ProjectId = projectId,
                    RunId = runId,
                    StepLogId = stepLogId,
                    TargetType = targetParts.Length > 0 ? targetParts[0] : "ALL",
                    TargetIndex = targetParts.Length > 1 && int.TryParse(targetParts[1], out var idx) ? idx : null,
                    Level = match.Groups[3].Value.Trim(),
                    Symptom = match.Groups[4].Value.Trim(),
                    Severity = match.Groups[5].Value.Trim(),
                    Evidence = match.Groups[6].Value.Trim(),
                    Status = "OPEN"
                };
                issues.Add(issue);
            }

            return issues;
        }
    }
}
