namespace LanguagePractice.Models
{
    public class Topic
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string Emotion { get; set; } = "";
        public string Scene { get; set; } = "";
        public string Tags { get; set; } = "";
        public string FixConditions { get; set; } = ""; // JSON or Text
        public string CreatedAt { get; set; } = "";
    }
}
