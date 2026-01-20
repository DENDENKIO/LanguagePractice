using System.Windows.Controls;
using LanguagePractice.ViewModels;

namespace LanguagePractice.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            // ViewModelをつなぐ
            DataContext = new SettingsViewModel();
        }
    }
}
