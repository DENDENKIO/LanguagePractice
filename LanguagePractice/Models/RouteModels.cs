using LanguagePractice.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LanguagePractice.Models
{
    // ルート定義
    public class RouteDefinition
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public List<RouteStep> Steps { get; set; } = new List<RouteStep>();
    }

    // ステップ定義
    public class RouteStep : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private int _stepNumber;
        public int StepNumber
        {
            get => _stepNumber;
            set { _stepNumber = value; OnPropertyChanged(); }
        }

        private string _title = "";
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        private OperationKind _operation;
        public OperationKind Operation
        {
            get => _operation;
            set { _operation = value; OnPropertyChanged(); }
        }

        // 入力バインディング
        public Dictionary<string, string> InputBindings { get; set; } = new Dictionary<string, string>();

        private LengthProfile? _fixedLength;
        public LengthProfile? FixedLength
        {
            get => _fixedLength;
            set { _fixedLength = value; OnPropertyChanged(); }
        }

        // ★追加/変更：FixedTone を通知プロパティ化（GIKOプリセット選択がここに保存される）
        private string? _fixedTone;
        public string? FixedTone
        {
            get => _fixedTone;
            set { _fixedTone = value; OnPropertyChanged(); }
        }
    }

    // 実行コンテキスト
    public class LpExecutionContext
    {
        public Dictionary<int, Dictionary<string, string>> StepOutputs { get; set; } = new Dictionary<int, Dictionary<string, string>>();
        public Dictionary<int, long> StepWorkIds { get; set; } = new Dictionary<int, long>();

        public void AddOutput(int stepNumber, Dictionary<string, string> output, long? workId = null)
        {
            StepOutputs[stepNumber] = output;

            if (workId.HasValue)
                StepWorkIds[stepNumber] = workId.Value;
        }

        public string GetPreviousOutputValue(string key)
        {
            for (int i = 100; i >= 1; i--)
            {
                if (StepOutputs.TryGetValue(i, out var dict) && dict.TryGetValue(key, out var val))
                    return val;
            }
            return "";
        }

        public long? GetPreviousWorkId()
        {
            for (int i = 100; i >= 1; i--)
            {
                if (StepWorkIds.TryGetValue(i, out var id))
                    return id;
            }
            return null;
        }
    }
}
