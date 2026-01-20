using System.Windows;
using LanguagePractice.ViewModels;

namespace LanguagePractice
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // ViewModelをセット
            DataContext = new MainViewModel();
        }
    }
}
