using System;
using System.Collections.Generic;
using System.Linq;

namespace LanguagePractice.Models
{
    // ========== Day ==========
    public class MsDay
    {
        public int Id { get; set; }
        public string DateKey { get; set; } = string.Empty;
        public string FocusMindsets { get; set; } = string.Empty;
        public string Scene { get; set; } = string.Empty;
        public string? StartRitual { get; set; }
        public string? EndRitual { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;

        public List<int> GetFocusMindsetList()
        {
            if (string.IsNullOrEmpty(FocusMindsets)) return new List<int>();
            return FocusMindsets.Split(new[] { ',', '、' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out int n) ? n : 0)
                .Where(n => n >= 1 && n <= 6)
                .ToList();
        }
    }

    // ========== Entry ==========
    public class MsEntry
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public string EntryType { get; set; } = string.Empty;
        public string BodyText { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    // ========== AiStepLog ==========
    public class MsAiStepLog
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string PromptText { get; set; } = string.Empty;
        public string RawOutput { get; set; } = string.Empty;
        public string ParsedJson { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string? FinishedAt { get; set; }
    }

    // ========== Review ==========
    public class MsReview
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public int TotalScore { get; set; }
        public string SubscoresJson { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
        public string Weaknesses { get; set; } = string.Empty;
        public string NextDayPlan { get; set; } = string.Empty;
        public string CoreLink { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    // ========== ExportLog ==========
    public class MsExportLog
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    // ========== AI結果モデル ==========
    public class MsPlanResult
    {
        public List<int> FocusMindsets { get; set; } = new();
        public string Scene { get; set; } = string.Empty;
        public List<string> Tasks { get; set; } = new();
        public string? StartRitual { get; set; }
        public string? EndRitual { get; set; }
    }

    public class MsReviewResult
    {
        public int TotalScore { get; set; }
        public Dictionary<string, int> Subscores { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public string NextDayPlan { get; set; } = string.Empty;
        public string CoreLink { get; set; } = string.Empty;
    }

    // ========== ドリル定義 ==========
    public class DrillDef
    {
        public string EntryType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Hint { get; set; } = string.Empty;
    }

    public class MindsetInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public List<DrillDef> Drills { get; set; } = new();
    }

    // ========== マインドセット定義 ==========
    public static class MindsetDefinitions
    {
        public static readonly Dictionary<int, MindsetInfo> All = new()
        {
            {
                1, new MindsetInfo
                {
                    Id = 1,
                    Name = "世界を素材として見る",
                    ShortName = "素材化",
                    Drills = new List<DrillDef>
                    {
                        new() { EntryType = "A1_TITLE", Title = "場面にタイトル", Hint = "最低3つ" },
                        new() { EntryType = "A2_VIEWPOINT", Title = "視点3通り", Hint = "1人称/三人称/物の視点" },
                        new() { EntryType = "A3_WHY5", Title = "Why×5", Hint = "1テーマを深掘り" }
                    }
                }
            },
            {
                2, new MindsetInfo
                {
                    Id = 2,
                    Name = "比喩で翻訳",
                    ShortName = "比喩",
                    Drills = new List<DrillDef>
                    {
                        new() { EntryType = "B1_METAPHOR", Title = "新しい比喩を1つ", Hint = "比喩で翻訳" },
                        new() { EntryType = "B2_DESTROY", Title = "既存比喩を壊して3変形", Hint = "比喩で翻訳" },
                        new() { EntryType = "B3_ABSTRACT", Title = "抽象→具体物", Hint = "孤独/不安/希望など" }
                    }
                }
            },
            {
                3, new MindsetInfo
                {
                    Id = 3,
                    Name = "観察を対話として扱う",
                    ShortName = "観察",
                    Drills = new List<DrillDef>
                    {
                        new() { EntryType = "C1_OBSERVE10", Title = "1物10分観察", Hint = "形/質感/語りかけ" },
                        new() { EntryType = "C2_NEGATIVE", Title = "ネガティブ・スペース記述", Hint = "間・距離・余白" },
                        new() { EntryType = "C3_QUESTION", Title = "対象に質問", Hint = "最低3問" }
                    }
                }
            },
            {
                4, new MindsetInfo
                {
                    Id = 4,
                    Name = "経験を錬金術で変換",
                    ShortName = "錬金術",
                    Drills = new List<DrillDef>
                    {
                        new() { EntryType = "D1_ALCHEMY", Title = "3層記録", Hint = "事実/感情/普遍" },
                        new() { EntryType = "D2_SYNESTHESIA", Title = "感情→色/音/触感へ変換", Hint = "共感覚的変換" },
                        new() { EntryType = "D3_FAILURE", Title = "失敗を素材化", Hint = "物語の一部にする" }
                    }
                }
            },
            {
                5, new MindsetInfo
                {
                    Id = 5,
                    Name = "メタ認知（第二の自分）",
                    ShortName = "メタ認知",
                    Drills = new List<DrillDef>
                    {
                        new() { EntryType = "E1_NOWLOG", Title = "今なにしてる？ログ", Hint = "3回以上、目標10回" },
                        new() { EntryType = "E2_FRIEND", Title = "友人に助言するなら？", Hint = "客観視" },
                        new() { EntryType = "E3_SCORE", Title = "10点採点＋理由3つ", Hint = "自己評価" }
                    }
                }
            },
            {
                6, new MindsetInfo
                {
                    Id = 6,
                    Name = "ルーティンを儀式化",
                    ShortName = "儀式化",
                    Drills = new List<DrillDef>
                    {
                        new() { EntryType = "F1_SANCTUARY", Title = "聖域定義", Hint = "場所/条件" },
                        new() { EntryType = "F2_START", Title = "始まりの儀式", Hint = "固定動作3つ" },
                        new() { EntryType = "F3_END", Title = "終わりの儀式", Hint = "固定動作2つ" },
                        new() { EntryType = "F4_PLAN", Title = "明日の実行計画", Hint = "時間ブロック" }
                    }
                }
            }
        };

        /// <summary>
        /// マインドセット名を取得
        /// </summary>
        public static string GetMindsetName(int id)
        {
            return All.TryGetValue(id, out var info) ? info.Name : $"マインドセット{id}";
        }

        /// <summary>
        /// マインドセットの短縮名を取得
        /// </summary>
        public static string GetMindsetShortName(int id)
        {
            return All.TryGetValue(id, out var info) ? info.ShortName : $"M{id}";
        }

        /// <summary>
        /// 全ドリルを取得
        /// </summary>
        public static List<DrillDef> GetAllDrills()
        {
            return All.Values.SelectMany(m => m.Drills).ToList();
        }

        /// <summary>
        /// 特定マインドセットのドリルを取得
        /// </summary>
        public static List<DrillDef> GetDrillsByMindset(int mindsetId)
        {
            return All.TryGetValue(mindsetId, out var info) ? info.Drills : new List<DrillDef>();
        }
    }
}
