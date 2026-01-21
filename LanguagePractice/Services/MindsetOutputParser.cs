using LanguagePractice.Helpers;
using LanguagePractice.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LanguagePractice.Services
{
    /// <summary>
    /// MindsetLab用AIアウトプットパーサー
    /// 仕様書2 第5.2章準拠
    /// </summary>
    public class MindsetOutputParser
    {
        /// <summary>
        /// MS_PLAN_GEN出力をパース
        /// </summary>
        public MsPlanResult? ParsePlanGen(string rawOutput)
        {
            if (string.IsNullOrWhiteSpace(rawOutput)) return null;

            try
            {
                string content = rawOutput;

                // マーカーで囲まれた部分を抽出
                var beginMatch = Regex.Match(rawOutput, Regex.Escape(LpConstants.MS_PLAN_BEGIN));
                var endMatch = Regex.Match(rawOutput, Regex.Escape(LpConstants.MS_PLAN_END));

                if (beginMatch.Success && endMatch.Success && endMatch.Index > beginMatch.Index)
                {
                    content = rawOutput.Substring(
                        beginMatch.Index + beginMatch.Length,
                        endMatch.Index - beginMatch.Index - beginMatch.Length
                    ).Trim();
                }

                return ParsePlanGenContent(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ParsePlanGen error: {ex.Message}");
                return null;
            }
        }

        private MsPlanResult ParsePlanGenContent(string content)
        {
            var result = new MsPlanResult();

            // 全体をデバッグ出力
            System.Diagnostics.Debug.WriteLine($"=== ParsePlanGenContent ===");
            System.Diagnostics.Debug.WriteLine(content);
            System.Diagnostics.Debug.WriteLine($"=== END ===");

            // FOCUS_MINDSETS: 数字をカンマ区切りで
            var focusMatch = Regex.Match(content, @"FOCUS_MINDSETS:\s*([0-9,\s]+)", RegexOptions.IgnoreCase);
            if (focusMatch.Success)
            {
                var nums = focusMatch.Groups[1].Value.Trim();
                foreach (var s in nums.Split(new[] { ',', '、', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(s.Trim(), out int n) && n >= 1 && n <= 6)
                    {
                        result.FocusMindsets.Add(n);
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine($"FocusMindsets: {string.Join(",", result.FocusMindsets)}");

            // TASKS: - で始まる行を収集
            var tasksMatch = Regex.Match(content, @"TASKS:\s*\n?((?:[-•＊・]\s*.+[\r\n]*)+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (tasksMatch.Success)
            {
                var tasksBlock = tasksMatch.Groups[1].Value;
                var taskLines = Regex.Matches(tasksBlock, @"[-•＊・]\s*(.+?)(?=[\r\n]|$)");
                foreach (Match m in taskLines)
                {
                    var task = m.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(task) && !task.StartsWith("START_") && !task.StartsWith("END_"))
                    {
                        result.Tasks.Add(task);
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine($"Tasks count: {result.Tasks.Count}");

            // START_RITUAL: 1行または複数行
            var startMatch = Regex.Match(content, @"START_RITUAL:\s*(.+?)(?=\n\s*(?:END_RITUAL|TASKS|FOCUS_|$))", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (startMatch.Success)
            {
                result.StartRitual = CleanValue(startMatch.Groups[1].Value);
            }
            else
            {
                // シンプルな1行パターン
                var startSimple = Regex.Match(content, @"START_RITUAL:\s*(.+)", RegexOptions.IgnoreCase);
                if (startSimple.Success)
                {
                    result.StartRitual = CleanValue(startSimple.Groups[1].Value.Split('\n')[0]);
                }
            }
            System.Diagnostics.Debug.WriteLine($"StartRitual: {result.StartRitual}");

            // END_RITUAL: 1行または複数行
            var endMatch = Regex.Match(content, @"END_RITUAL:\s*(.+?)(?=\n\s*(?:START_RITUAL|TASKS|FOCUS_|$))", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (endMatch.Success)
            {
                result.EndRitual = CleanValue(endMatch.Groups[1].Value);
            }
            else
            {
                // シンプルな1行パターン
                var endSimple = Regex.Match(content, @"END_RITUAL:\s*(.+)", RegexOptions.IgnoreCase);
                if (endSimple.Success)
                {
                    result.EndRitual = CleanValue(endSimple.Groups[1].Value.Split('\n')[0]);
                }
            }
            System.Diagnostics.Debug.WriteLine($"EndRitual: {result.EndRitual}");

            return result;
        }

        /// <summary>
        /// 値をクリーンアップ
        /// </summary>
        private string CleanValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            // 前後の空白、改行を除去
            value = value.Trim();

            // 末尾のマーカーを除去
            value = Regex.Replace(value, @"---END.*$", "", RegexOptions.IgnoreCase).Trim();

            // センチネルを除去
            value = value.Replace(LpConstants.DONE_SENTINEL, "").Trim();

            return value;
        }

        /// <summary>
        /// MS_REVIEW_SCORE出力をパース
        /// </summary>
        public MsReviewResult? ParseReviewScore(string rawOutput)
        {
            if (string.IsNullOrWhiteSpace(rawOutput)) return null;

            try
            {
                string content = rawOutput;

                // マーカーで囲まれた部分を抽出
                var beginMatch = Regex.Match(rawOutput, Regex.Escape(LpConstants.MS_REVIEW_BEGIN));
                var endMatch = Regex.Match(rawOutput, Regex.Escape(LpConstants.MS_REVIEW_END));

                if (beginMatch.Success && endMatch.Success && endMatch.Index > beginMatch.Index)
                {
                    content = rawOutput.Substring(
                        beginMatch.Index + beginMatch.Length,
                        endMatch.Index - beginMatch.Index - beginMatch.Length
                    ).Trim();
                }

                return ParseReviewScoreContent(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ParseReviewScore error: {ex.Message}");
                return null;
            }
        }

        private MsReviewResult ParseReviewScoreContent(string content)
        {
            var result = new MsReviewResult();

            // TOTAL_SCORE
            var totalMatch = Regex.Match(content, @"TOTAL_SCORE:\s*(\d+)", RegexOptions.IgnoreCase);
            if (totalMatch.Success) result.TotalScore = int.Parse(totalMatch.Groups[1].Value);

            // SUBSCORES (A: 80, B: 70, etc.)
            var subscorePatterns = new[] { "A", "B", "C", "D", "E", "F" };
            foreach (var key in subscorePatterns)
            {
                // "A: 80" または "A：80" のパターン
                var scoreMatch = Regex.Match(content, $@"(?<![A-Z]){key}\s*[:：]\s*(\d+)", RegexOptions.IgnoreCase);
                if (scoreMatch.Success)
                {
                    result.Subscores[key] = int.Parse(scoreMatch.Groups[1].Value);
                }
            }

            // STRENGTHS
            var strengthsMatch = Regex.Match(content, @"STRENGTHS:\s*\n?((?:[-•＊・]\s*.+[\r\n]*)+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (strengthsMatch.Success)
            {
                var block = strengthsMatch.Groups[1].Value;
                var items = Regex.Matches(block, @"[-•＊・]\s*(.+?)(?=[\r\n]|$)");
                foreach (Match m in items)
                {
                    var item = m.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(item) && !item.StartsWith("WEAK"))
                    {
                        result.Strengths.Add(item);
                    }
                }
            }

            // WEAKNESSES
            var weaknessesMatch = Regex.Match(content, @"WEAKNESSES:\s*\n?((?:[-•＊・]\s*.+[\r\n]*)+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (weaknessesMatch.Success)
            {
                var block = weaknessesMatch.Groups[1].Value;
                var items = Regex.Matches(block, @"[-•＊・]\s*(.+?)(?=[\r\n]|$)");
                foreach (Match m in items)
                {
                    var item = m.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(item) && !item.StartsWith("NEXT_") && !item.StartsWith("CORE_"))
                    {
                        result.Weaknesses.Add(item);
                    }
                }
            }

            // NEXT_DAY_PLAN
            var nextMatch = Regex.Match(content, @"NEXT_DAY_PLAN:\s*(.+?)(?=\n\s*(?:CORE_LINK|STRENGTHS|WEAKNESSES|$))", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (nextMatch.Success)
            {
                result.NextDayPlan = CleanValue(nextMatch.Groups[1].Value);
            }
            else
            {
                var nextSimple = Regex.Match(content, @"NEXT_DAY_PLAN:\s*(.+)", RegexOptions.IgnoreCase);
                if (nextSimple.Success)
                {
                    result.NextDayPlan = CleanValue(nextSimple.Groups[1].Value.Split('\n')[0]);
                }
            }

            // CORE_LINK
            var coreMatch = Regex.Match(content, @"CORE_LINK:\s*(.+?)(?=\n\s*(?:NEXT_DAY|STRENGTHS|WEAKNESSES|$))", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (coreMatch.Success)
            {
                result.CoreLink = CleanValue(coreMatch.Groups[1].Value);
            }
            else
            {
                var coreSimple = Regex.Match(content, @"CORE_LINK:\s*(.+)", RegexOptions.IgnoreCase);
                if (coreSimple.Success)
                {
                    result.CoreLink = CleanValue(coreSimple.Groups[1].Value.Split('\n')[0]);
                }
            }

            return result;
        }

        /// <summary>
        /// MsReviewResultをJSON文字列に変換
        /// </summary>
        public string ToJson(MsReviewResult result)
        {
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// MsPlanResultをJSON文字列に変換
        /// </summary>
        public string ToJson(MsPlanResult result)
        {
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// SubscoresをJSON文字列に変換
        /// </summary>
        public string SubscoresToJson(Dictionary<string, int> subscores)
        {
            return JsonSerializer.Serialize(subscores);
        }
    }
}
