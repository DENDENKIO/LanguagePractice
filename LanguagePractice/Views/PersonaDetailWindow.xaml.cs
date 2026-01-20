using System.Windows;
using LanguagePractice.Models;
using LanguagePractice.ViewModels;

namespace LanguagePractice.Views
{
    public partial class PersonaDetailWindow : Window
    {
        public PersonaDetailWindow(Persona persona)
        {
            InitializeComponent();
            DataContext = new PersonaDetailViewModel(persona);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
