using System.Collections.Generic;

namespace LanguagePractice.Models
{
    public class Experiment
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string CreatedAt { get; set; } = "";

        // 変数名 (例: "Reader", "Tone")
        public string VariableName { get; set; } = "";

        // 共通設定 (JSON等で保存も可だが、今回は簡易的にテキスト保持)
        public string CommonTopic { get; set; } = "";
        public string CommonWriter { get; set; } = "";
    }

    public class ExperimentTrial
    {
        public long Id { get; set; }
        public long ExperimentId { get; set; }

        // 変数値 (例: "疲れた社会人", "平安調")
        public string VariableValue { get; set; } = "";

        // 結果WorkのID
        public long? ResultWorkId { get; set; }

        // 採点メモ
        public string Rating { get; set; } = "";
    }
}
