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
    /// </summary>
    public class MindsetPromptBuilder
    {
        private static readonly Random _random = new Random();

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
            sb.AppendLine("6. ルーティンを儀式化（聖域定義/時間ブロック/計画）");
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
            sb.AppendLine("【重要な指示】");
            sb.AppendLine("1. FOCUS_MINDSETSは毎回異なる2〜3個をランダムに選んでください（1〜6から）");
            sb.AppendLine("2. 前回と同じ組み合わせは避けてください");
            sb.AppendLine("3. SCENEには「日常的で具体的なシーン」を設定してください（カフェ、電車、公園、スーパー、自宅リビング等）");
            sb.AppendLine("4. ユーザーはPCの前にいることが多いため、想像でそのシーンに入り込んでトレーニングします");
            sb.AppendLine();
            sb.AppendLine("【出力形式】以下の形式を厳守してください。例文をそのまま出力しないこと。");
            sb.AppendLine();
            sb.AppendLine(LpConstants.MS_PLAN_BEGIN);
            sb.AppendLine("FOCUS_MINDSETS: （1〜6から2〜3個をカンマ区切りで。例: 1,4,6）");
            sb.AppendLine("SCENE: （今日の訓練シーン。日常的な場所と状況を具体的に描写。50〜100字程度）");
            sb.AppendLine("TASKS:");
            sb.AppendLine("- （シーンに基づいた具体的なタスク1）");
            sb.AppendLine("- （シーンに基づいた具体的なタスク2）");
            sb.AppendLine("- （シーンに基づいた具体的なタスク3）");
            sb.AppendLine(LpConstants.MS_PLAN_END);
            sb.AppendLine();
            sb.AppendLine("【出力例】参考にして、異なる内容を生成すること");
            sb.AppendLine(LpConstants.MS_PLAN_BEGIN);
            sb.AppendLine("FOCUS_MINDSETS: 1,3,4");
            sb.AppendLine("SCENE: 平日の朝8時、最寄り駅のホームで電車を待っている。周囲にはスーツ姿の会社員、制服の学生、買い物袋を持った高齢者がいる。電車の到着アナウンスが響く。");
            sb.AppendLine("TASKS:");
            sb.AppendLine("- ホームで待つ人々の中から1人を選び、その人の「今日の物語」に3つのタイトルをつける");
            sb.AppendLine("- 電車が到着する瞬間を「線路の視点」「風の視点」「時計の視点」で描写する");
            sb.AppendLine("- 自分が感じた「朝の通勤の空気感」を事実/感情/普遍の3層で記録する");
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
                var mindsetNames = day.GetFocusMindsetList()
                    .Select(id => $"{id}. {MindsetDefinitions.GetMindsetName(id)}")
                    .ToList();
                sb.AppendLine($"【重点マインドセット】{string.Join(" / ", mindsetNames)}");
            }

            if (!string.IsNullOrEmpty(day.Scene))
            {
                sb.AppendLine($"【今日のシーン】{day.Scene}");
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
            sb.AppendLine("【出力形式】以下の形式を厳守してください。");
            sb.AppendLine();
            sb.AppendLine(LpConstants.MS_REVIEW_BEGIN);
            sb.AppendLine("TOTAL_SCORE: （0〜100の数値）");
            sb.AppendLine("SUBSCORES:");
            sb.AppendLine("  A: （世界を素材として見る の点数）");
            sb.AppendLine("  B: （比喩で翻訳 の点数）");
            sb.AppendLine("  C: （観察を対話として扱う の点数）");
            sb.AppendLine("  D: （経験を錬金術で変換 の点数）");
            sb.AppendLine("  E: （メタ認知 の点数）");
            sb.AppendLine("  F: （ルーティンを儀式化 の点数）");
            sb.AppendLine("STRENGTHS:");
            sb.AppendLine("- （強み1）");
            sb.AppendLine("- （強み2）");
            sb.AppendLine("WEAKNESSES:");
            sb.AppendLine("- （改善点1）");
            sb.AppendLine("- （改善点2）");
            sb.AppendLine("NEXT_DAY_PLAN: （明日の具体的な課題提案）");
            sb.AppendLine("CORE_LINK: （今日の記録から抽出した「核」候補：主題/感情/問い）");
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
