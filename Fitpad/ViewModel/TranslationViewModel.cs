using System.Threading.Tasks;
using Fitpad.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class TranslationViewModel : INotifyPropertyChanged
    {
        private readonly Translator _translator;

        private string _inputText;
        public string InputText
        {
            get => _inputText;
            set
            {
                _inputText = value;
                OnPropertyChanged();
            }
        }

        private string _translatedText;
        public string TranslatedText
        {
            get => _translatedText;
            set
            {
                _translatedText = value;
                OnPropertyChanged();
            }
        }

        public TranslationViewModel()
        {
            _translator = new Translator();
        }

        public async Task TranslateAsync()
        {
            if (!string.IsNullOrWhiteSpace(InputText))
            {
                // Only specify target language (source language defaults to "en")
                string translatedText = await _translator.TranslateAsync(InputText, "uk");
                TranslatedText = translatedText;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
