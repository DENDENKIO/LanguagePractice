using System.Windows.Controls;
using LanguagePractice.ViewModels;

namespace LanguagePractice.Views
{
    public partial class WorkbenchView : UserControl
    {
        public WorkbenchView()
        {
            InitializeComponent();
            // DataContext = new WorkbenchViewModel(); // これはMainWindow側で行う
        }
    }
}
