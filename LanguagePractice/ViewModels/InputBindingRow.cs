using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LanguagePractice.ViewModels
{
    public enum BindingValueMode
    {
        MANUAL,
        PREV_OUTPUT,
        FIXED
    }

    /// <summary>
    /// RouteStep.InputBindings の1行をGUI編集するための行VM
    /// </summary>
    public class InputBindingRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _inputKey = "";
        public string InputKey
        {
            get => _inputKey;
            set { _inputKey = value ?? ""; OnPropertyChanged(); OnPropertyChanged(nameof(BindingString)); }
        }

        private BindingValueMode _mode = BindingValueMode.MANUAL;
        public BindingValueMode Mode
        {
            get => _mode;
            set { _mode = value; OnPropertyChanged(); OnPropertyChanged(nameof(BindingString)); }
        }

        private string _value = "";
        /// <summary>
        /// Mode=PREV_OUTPUT のときは「参照する出力キー」
        /// Mode=FIXED のときは「固定文字列」
        /// Mode=MANUAL のときは未使用（空でOK）
        /// </summary>
        public string Value
        {
            get => _value;
            set { _value = value ?? ""; OnPropertyChanged(); OnPropertyChanged(nameof(BindingString)); }
        }

        /// <summary>
        /// 既存実装互換の保存形式へ変換
        /// </summary>
        public string BindingString
        {
            get
            {
                return Mode switch
                {
                    BindingValueMode.MANUAL => "MANUAL",
                    BindingValueMode.PREV_OUTPUT => $"PREV_OUTPUT:{Value}".TrimEnd(':'),
                    BindingValueMode.FIXED => $"FIXED:{Value}".TrimEnd(':'),
                    _ => "MANUAL"
                };
            }
        }

        public static InputBindingRow FromBinding(string key, string binding)
        {
            key ??= "";
            binding ??= "";

            var row = new InputBindingRow { InputKey = key };

            if (binding.Equals("MANUAL", StringComparison.OrdinalIgnoreCase))
            {
                row.Mode = BindingValueMode.MANUAL;
                row.Value = "";
                return row;
            }

            if (binding.StartsWith("PREV_OUTPUT:", StringComparison.OrdinalIgnoreCase))
            {
                row.Mode = BindingValueMode.PREV_OUTPUT;
                row.Value = binding.Substring("PREV_OUTPUT:".Length);
                return row;
            }

            if (binding.StartsWith("FIXED:", StringComparison.OrdinalIgnoreCase))
            {
                row.Mode = BindingValueMode.FIXED;
                row.Value = binding.Substring("FIXED:".Length);
                return row;
            }

            // 互換：未知形式は FIXED として保持
            row.Mode = BindingValueMode.FIXED;
            row.Value = binding;
            return row;
        }
    }
}
