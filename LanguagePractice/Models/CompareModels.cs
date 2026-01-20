namespace LanguagePractice.Models
{
    public class CompareSet
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string Note { get; set; } = "";
        public long? WinnerWorkId { get; set; }
        public string CreatedAt { get; set; } = "";
    }

    public class CompareItem
    {
        public long Id { get; set; }
        public long CompareSetId { get; set; }
        public long WorkId { get; set; }
        public string Position { get; set; } = ""; // "Left", "Right", "Middle"
    }
}
