using LanguagePractice.Helpers;
using LanguagePractice.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace LanguagePractice.Views
{
    public partial class LibraryPickerDialog : Window
    {
        public object? SelectedItem { get; private set; }

        public LibraryPickerDialog(string title, IEnumerable<object> items, PickerMode mode)
        {
            InitializeComponent();
            DataContext = new { Title = title };
            MainGrid.ItemsSource = items;

            GenerateColumns(mode);
        }

        private static Style MakeBoldTextStyle()
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            return style;
        }

        private void GenerateColumns(PickerMode mode)
        {
            MainGrid.Columns.Clear();

            var boldText = MakeBoldTextStyle();

            // ID列 (共通)
            MainGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new Binding("Id"),
                Width = 40
            });

            if (mode == PickerMode.Topic)
            {
                MainGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "お題",
                    Binding = new Binding("Title"),
                    Width = 250,
                    ElementStyle = boldText
                });

                MainGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "感情",
                    Binding = new Binding("Emotion"),
                    Width = 100
                });

                MainGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "タグ",
                    Binding = new Binding("Tags"),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                });
            }
            else if (mode == PickerMode.Persona)
            {
                MainGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "名前",
                    Binding = new Binding("Name"),
                    Width = 150,
                    ElementStyle = boldText
                });

                MainGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Bio",
                    Binding = new Binding("Bio"),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                });
            }
            else if (mode == PickerMode.Work)
            {
                MainGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "タイトル",
                    Binding = new Binding("Title"),
                    Width = 200,
                    ElementStyle = boldText
                });

                MainGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "本文 (冒頭)",
                    Binding = new Binding("BodyText"),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                });
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            CommitSelection();
        }

        private void MainGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CommitSelection();
        }

        private void CommitSelection()
        {
            SelectedItem = MainGrid.SelectedItem;
            if (SelectedItem != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("項目を選択してください。");
            }
        }
    }

    public enum PickerMode
    {
        Topic,
        Persona,
        Work
    }
}
