namespace LanguagePractice.Models
{
    public class Observation
    {
        public long Id { get; set; }
        public string ImageUrl { get; set; } = "";
        public string Motif { get; set; } = "";
        public string VisualRaw { get; set; } = "";
        public string SoundRaw { get; set; } = "";
        public string MetaphorsRaw { get; set; } = "";
        public string CoreCandidatesRaw { get; set; } = "";
        public string FullContent { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }
}
