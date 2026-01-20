using System.Windows;

namespace LanguagePractice.Views
{
    // 単純なデータ保持用クラス
    public class DetailData
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
    }

    public partial class DetailWindow : Window
    {
        public DetailWindow(string title, string content)
        {
            InitializeComponent();
            // 匿名型ではなく、普通のクラスのインスタンスをセット
            DataContext = new DetailData { Title = title, Content = content };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
