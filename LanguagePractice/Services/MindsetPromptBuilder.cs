using LanguagePractice.Helpers;
using LanguagePractice.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LanguagePractice.Services
{
    /// <summary>
    /// MindsetLab用AIプロンプトビルダー
    /// 仕様書2 第4.3章 / 第5.2章準拠
    /// BrowserWindow自動取得対応（DONE_SENTINEL使用）
    /// </summary>
    public class MindsetPromptBuilder
    {
        /// <summary>
        /// MS_PLAN_GEN: 今日の重点ミッション生成
        /// </summary>
        public string BuildPlanGenPrompt(int? consecutiveDays = null, string? previousWeakness = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("あなたは「MindsetLab」の訓練コーチです。");
            sb.AppendLine("ユーザーの「凡人ではないレベル」への到達を支援するため、今日の訓練ミッションを生成してください。");
            sb.AppendLine();
            sb.AppendLine("【6つのマインドセット】");
            sb.AppendLine("1. 世界を素材として見る（タイトル付け/視点変換/Why掘り）");
            sb.AppendLine("2. 比喩で翻訳（新比喩/破壊再構築/抽象→具体）");
            sb.AppendLine("3. 観察を対話として扱う（10分観察/余白記述/質問）");
            sb.AppendLine("4. 経験を錬金術で変換（3層記録/感情変換/失敗素材化）");
            sb.AppendLine("5. メタ認知（今なにしてる？ログ/友人視点/10点採点）");
            sb.AppendLine("6. ルーティンを儀式化（聖域/開始儀式/終了儀式/計画）");
            sb.AppendLine();

            if (consecutiveDays.HasValue && consecutiveDays > 0)
            {
                sb.AppendLine($"【現在の継続日数】{consecutiveDays}日");
            }

            if (!string.IsNullOrEmpty(previousWeakness))
            {
                sb.AppendLine($"【前回の弱点】{previousWeakness}");
                sb.AppendLine("→ 今回は特にこの弱点を強化するタスクを含めてください。");
            }

            sb.AppendLine();
            sb.AppendLine("【出力形式】必ず以下の形式で出力してください。マーカーは正確に記述してください。");
            sb.AppendLine();
            sb.AppendLine(LpConstants.MS_PLAN_BEGIN);
            sb.AppendLine("FOCUS_MINDSETS: 2,3,5");
            sb.AppendLine("TASKS:");
            sb.AppendLine("- タスク1の具体的な内容");
            sb.AppendLine("- タスク2の具体的な内容");
            sb.AppendLine("- タスク3の具体的な内容");
            sb.AppendLine("START_RITUAL: 開始儀式の提案（例：深呼吸3回→目を閉じて今日の目標を唱える）");
            sb.AppendLine("END_RITUAL: 終了儀式の提案（例：今日学んだこと3つを書き出す→水を一杯飲む）");
            sb.AppendLine(LpConstants.MS_PLAN_END);
            sb.AppendLine();
            sb.AppendLine($"出力の最後に必ず {LpConstants.DONE_SENTINEL} を付けてください。");

            return sb.ToString();
        }

        /// <summary>
        /// MS_REVIEW_SCORE: レビュー生成
        /// </summary>
        public string BuildReviewPrompt(MsDay day, List<MsEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("あなたは「MindsetLab」の訓練コーチです。");
            sb.AppendLine("ユーザーの今日の訓練記録を評価し、フィードバックを生成してください。");
            sb.AppendLine();
            sb.AppendLine("【グローバル・リビジョンの核（参照）】");
            sb.AppendLine("- 主題（何について）");
            sb.AppendLine("- 中心感情・態度（どう感じ/どう構える）");
            sb.AppendLine("- 読者に渡す変化/問い（何を残すか）");
            sb.AppendLine();
            sb.AppendLine($"【今日の日付】{day.DateKey}");

            if (!string.IsNullOrEmpty(day.FocusMindsets))
            {
                sb.AppendLine($"【重点マインドセット】{day.FocusMindsets}");
            }
            sb.AppendLine();
            sb.AppendLine("【提出された記録】");

            if (entries.Count == 0)
            {
                sb.AppendLine("(入力なし)");
            }
            else
            {
                foreach (var entry in entries.OrderBy(e => e.EntryType))
                {
                    var drillTitle = GetDrillTitle(entry.EntryType);
                    sb.AppendLine($"## {entry.EntryType} ({drillTitle})");
                    sb.AppendLine(entry.BodyText);
                    sb.AppendLine();
                }
            }

            sb.AppendLine("【評価基準】");
            sb.AppendLine("- 各マインドセット：具体性、独自性、深掘り度、核への接続可能性");
            sb.AppendLine("- 総合点：100点満点");
            sb.AppendLine();
            sb.AppendLine("【出力形式】必ず以下の形式で出力してください。マーカーは正確に記述してください。");
            sb.AppendLine();
            sb.AppendLine(LpConstants.MS_REVIEW_BEGIN);
            sb.AppendLine("TOTAL_SCORE: 75");
            sb.AppendLine("SUBSCORES:");
            sb.AppendLine("  A: 80");
            sb.AppendLine("  B: 70");
            sb.AppendLine("  C: 75");
            sb.AppendLine("  D: 70");
            sb.AppendLine("  E: 80");
            sb.AppendLine("  F: 75");
            sb.AppendLine("STRENGTHS:");
            sb.AppendLine("- 強み1の具体的な内容");
            sb.AppendLine("- 強み2の具体的な内容");
            sb.AppendLine("WEAKNESSES:");
            sb.AppendLine("- 弱み1の具体的な内容");
            sb.AppendLine("- 弱み2の具体的な内容");
            sb.AppendLine("NEXT_DAY_PLAN: 明日の課題・意図的練習の提案");
            sb.AppendLine("CORE_LINK: 今日の記録から抽出した「核」候補（主題/感情/問い）");
            sb.AppendLine(LpConstants.MS_REVIEW_END);
            sb.AppendLine();
            sb.AppendLine($"出力の最後に必ず {LpConstants.DONE_SENTINEL} を付けてください。");

            return sb.ToString();
        }

        private string GetDrillTitle(string entryType)
        {
            foreach (var m in MindsetDefinitions.All.Values)
            {
                foreach (var d in m.Drills)
                {
                    if (d.EntryType == entryType) return d.Title;
                }
            }
            return entryType;
        }
    }
}
