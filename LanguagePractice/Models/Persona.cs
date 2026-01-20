using LanguagePractice.Helpers;

namespace LanguagePractice.Models
{
    public class Persona
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string Location { get; set; } = ""; // 活動場所・国籍
        public string Bio { get; set; } = "";      // 経歴・概要
        public string Style { get; set; } = "";    // 文体・特徴
        public string Tags { get; set; } = "";

        // 検証ステータス (UNVERIFIED, VERIFIED 等)
        public string VerificationStatus { get; set; } = Helpers.VerificationStatus.UNVERIFIED.ToString();

        public string CreatedAt { get; set; } = "";
    }
}
