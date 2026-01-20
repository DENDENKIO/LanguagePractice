namespace LanguagePractice.Models
{
    public class Work
    {
        public long Id { get; set; }
        public string Kind { get; set; } = "";     // WorkKind (TEXT_GEN, GIKO...)
        public string Title { get; set; } = "";    // Topicや自動生成タイトル
        public string BodyText { get; set; } = ""; // 本文
        public string CreatedAt { get; set; } = "";

        // 関連ID（NULL許容）
        public long? RunLogId { get; set; }

        // メタデータ（JSON等で保存も検討できますが、今回はフラットに）
        public string WriterName { get; set; } = "";
        public string ReaderNote { get; set; } = ""; // 読者像スナップショット
        public string ToneLabel { get; set; } = "";
    }
}
