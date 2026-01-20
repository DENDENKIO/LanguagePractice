using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LanguagePractice.ViewModels;

namespace LanguagePractice.Views
{
    public partial class LibraryView : UserControl
    {
        public LibraryView()
        {
            InitializeComponent();
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && DataContext is LibraryViewModel vm)
            {
                vm.ShowDetailCommand.Execute(row.Item);
            }
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not LibraryViewModel vm)
                return;

            // 現在タブの中身（DataGrid）を取得
            if (Tabs.SelectedItem is not TabItem tab) return;
            if (tab.Content is not DataGrid grid) return;

            IList selected = grid.SelectedItems;
            vm.DeleteItems(selected);
        }
    }
}
