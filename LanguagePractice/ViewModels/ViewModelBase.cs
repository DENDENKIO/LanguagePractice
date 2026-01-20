using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LanguagePractice.ViewModels
{
    // 画面のデータ変更を通知するための基底クラス
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
