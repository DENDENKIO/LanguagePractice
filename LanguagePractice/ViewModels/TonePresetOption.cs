namespace LanguagePractice.ViewModels
{
    public class TonePresetOption
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = ""; // 保存値（FixedToneに入る）
        public override string ToString() => Label;
    }
}
