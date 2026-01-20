namespace LanguagePractice.Models
{
    public class PersonaVerification
    {
        public long Id { get; set; }
        public long PersonaId { get; set; }
        public string CreatedAt { get; set; } = "";

        // 入力された根拠
        public string Evidence1 { get; set; } = "";
        public string Evidence2 { get; set; } = "";
        public string Evidence3 { get; set; } = "";

        // AIの判定結果 (JSONまたはテキスト全文)
        public string ResultJson { get; set; } = "";
        public string OverallVerdict { get; set; } = ""; // SUPPORTED, CONTRADICTED etc.

        // 修正案
        public string RevisedBioDraft { get; set; } = "";
    }
}
