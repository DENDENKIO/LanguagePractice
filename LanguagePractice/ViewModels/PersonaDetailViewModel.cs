using LanguagePractice.Helpers;
using LanguagePractice.Models;
using LanguagePractice.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace LanguagePractice.ViewModels
{
    public class PersonaDetailViewModel : ViewModelBase
    {
        private readonly PersonaService _personaService;
        private readonly PersonaVerificationService _verifyService;

        public Persona Persona { get; }
        public ObservableCollection<PersonaVerification> VerificationHistory { get; } = new ObservableCollection<PersonaVerification>();

        public ICommand SaveChangesCommand { get; }

        public PersonaDetailViewModel(Persona persona)
        {
            Persona = persona;
            _personaService = new PersonaService();
            _verifyService = new PersonaVerificationService();

            SaveChangesCommand = new RelayCommand(ExecuteSave);

            LoadHistory();
        }

        private void LoadHistory()
        {
            var history = _verifyService.GetHistory(Persona.Id);
            foreach (var h in history) VerificationHistory.Add(h);
        }

        private void ExecuteSave()
        {
            // 更新ロジックは本来ServiceにUpdateメソッドが必要
            // ここでは簡易的にメッセージのみ
            MessageBox.Show("編集内容はメモリ上には反映されましたが、DB更新機能はP2以降の実装となります。", "通知");
        }
    }
}
