namespace LanguagePractice.Models
{
    public class StudyCard
    {
        public long Id { get; set; }
        public long? SourceWorkId { get; set; }  // 元になったWork
        public string CreatedAt { get; set; } = "";

        // 解析結果をそのままJSONまたはテキスト塊として保存する簡易設計（MVP）
        // 本格的には各フィールド（BestExpressions等）を別テーブルに正規化しますが
        // MVPでは「解析されたテキスト全体」または「主要項目」をフラットに持ちます。

        public string Focus { get; set; } = "";
        public string Level { get; set; } = "";
        public string BestExpressionsRaw { get; set; } = "";
        public string MetaphorChainsRaw { get; set; } = "";
        public string DoNextRaw { get; set; } = "";
        public string Tags { get; set; } = "";

        // 全解析結果（Parserで辞書になったものをJSON等で持つのが理想だが、今回はテキストで）
        public string FullParsedContent { get; set; } = "";
    }
}
