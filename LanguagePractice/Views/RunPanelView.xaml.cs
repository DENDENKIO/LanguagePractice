using System.Windows.Controls;

namespace LanguagePractice.Views
{
    public partial class RunPanelView : UserControl
    {
        public RunPanelView()
        {
            InitializeComponent();
            // ViewModelは親画面からDataContext経由で渡される想定ですが、
            // 単体テスト用に空で置いておきます。
        }
    }
}
