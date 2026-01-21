using System;
using System.Collections.Generic;

namespace LanguagePractice.Models
{
    /// <summary>
    /// MindsetLab プロファイル
    /// </summary>
    public class MsProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// MindsetLab 日単位のセッション
    /// </summary>
    public class MsDay
    {
        public int Id { get; set; }
        public string DateKey { get; set; } = string.Empty; // 例: 2026-01-21
        public string FocusMindsets { get; set; } = string.Empty; // JSON: "1,2,5"
        public string StartRitual { get; set; } = string.Empty;
        public string EndRitual { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;

        // 表示用
        public List<int> GetFocusMindsetList()
        {
            if (string.IsNullOrEmpty(FocusMindsets)) return new List<int>();
            var result = new List<int>();
            foreach (var s in FocusMindsets.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(s.Trim(), out int n)) result.Add(n);
            }
            return result;
        }
    }

    /// <summary>
    /// MindsetLab ドリル入力項目
    /// </summary>
    public class MsEntry
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public string EntryType { get; set; } = string.Empty; // 例: A1_TITLE, B1_METAPHOR
        public string BodyText { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// MindsetLab AIステップログ
    /// </summary>
    public class MsAiStepLog
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public string StepName { get; set; } = string.Empty; // MS_PLAN_GEN, MS_REVIEW_SCORE等
        public string PromptText { get; set; } = string.Empty;
        public string RawOutput { get; set; } = string.Empty;
        public string ParsedJson { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // PENDING, DONE, ERROR
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// MindsetLab AIレビュー結果
    /// </summary>
    public class MsReview
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public int TotalScore { get; set; }
        public string SubscoresJson { get; set; } = string.Empty; // {"A":8,"B":7,...}
        public string Strengths { get; set; } = string.Empty;
        public string Weaknesses { get; set; } = string.Empty;
        public string NextDayPlan { get; set; } = string.Empty;
        public string CoreLink { get; set; } = string.Empty; // 主題/感情/問い候補
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// MindsetLab エクスポートログ
    /// </summary>
    public class MsExportLog
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// 6マインドセットの定義（固定）
    /// </summary>
    public static class MindsetDefinitions
    {
        public static readonly Dictionary<int, MindsetInfo> All = new()
        {
            { 1, new MindsetInfo(1, "世界を素材として見る", "日常を素材化して回収する", new[] {
                new DrillDef("A1_TITLE", "場面にタイトル", "最低3つ"),
                new DrillDef("A2_PERSPECTIVE", "視点3通り", "1人称/三人称/物の視点"),
                new DrillDef("A3_WHY5", "Why×5", "1テーマ")
            })},
            { 2, new MindsetInfo(2, "比喩で翻訳", "比喩を毎日生成・破壊・再構築する", new[] {
                new DrillDef("B1_METAPHOR", "新しい比喩を1つ", ""),
                new DrillDef("B2_DESTROY", "既存比喩を壊して3変形", ""),
                new DrillDef("B3_ABSTRACT", "抽象→具体物", "孤独/不安/希望など")
            })},
            { 3, new MindsetInfo(3, "観察を対話として扱う", "対話として行い描写素材を増やす", new[] {
                new DrillDef("C1_OBSERVE10", "1物10分観察", "形/質感/語りかけ"),
                new DrillDef("C2_NEGATIVE", "ネガティブ・スペース記述", "間・距離・余白"),
                new DrillDef("C3_QUESTION", "対象に質問", "最低3問")
            })},
            { 4, new MindsetInfo(4, "経験を錬金術で変換", "事実/感情/普遍で変換し核へ接続", new[] {
                new DrillDef("D1_ALCHEMY", "3層記録", "事実/感情/普遍"),
                new DrillDef("D2_SYNESTHESIA", "感情→色/音/触感へ変換", ""),
                new DrillDef("D3_FAILURE", "失敗を素材化", "物語の一部にする")
            })},
            { 5, new MindsetInfo(5, "メタ認知（第二の自分）", "第二の自分として稼働させ自己評価→改善", new[] {
                new DrillDef("E1_NOWLOG", "今日の「今なにしてる？」ログ", "3回でも可、目標10回"),
                new DrillDef("E2_FRIEND", "友人に助言するなら？", ""),
                new DrillDef("E3_SCORE", "10点採点＋理由3つ", "")
            })},
            { 6, new MindsetInfo(6, "ルーティンを儀式化", "儀式として設計し習慣化に乗せる", new[] {
                new DrillDef("F1_SANCTUARY", "聖域（場所/条件）定義", ""),
                new DrillDef("F2_START", "始まりの儀式", "固定動作3つ"),
                new DrillDef("F3_END", "終わりの儀式", "固定動作2つ"),
                new DrillDef("F4_PLAN", "明日の実行計画", "時間ブロック")
            })}
        };

        public static string GetMindsetName(int id) => All.TryGetValue(id, out var m) ? m.Name : $"Mindset {id}";
    }

    public class MindsetInfo
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; }
        public DrillDef[] Drills { get; }

        public MindsetInfo(int id, string name, string description, DrillDef[] drills)
        {
            Id = id;
            Name = name;
            Description = description;
            Drills = drills;
        }
    }

    public class DrillDef
    {
        public string EntryType { get; }
        public string Title { get; }
        public string Hint { get; }

        public DrillDef(string entryType, string title, string hint)
        {
            EntryType = entryType;
            Title = title;
            Hint = hint;
        }
    }

    /// <summary>
    /// AI生成用：今日のミッション
    /// </summary>
    public class MsPlanResult
    {
        public List<int> FocusMindsets { get; set; } = new();
        public List<string> Tasks { get; set; } = new();
        public string StartRitual { get; set; } = string.Empty;
        public string EndRitual { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI生成用：レビュー結果
    /// </summary>
    public class MsReviewResult
    {
        public int TotalScore { get; set; }
        public Dictionary<string, int> Subscores { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public string NextDayPlan { get; set; } = string.Empty;
        public string CoreLink { get; set; } = string.Empty;
    }
}
