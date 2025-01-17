using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class NewsViewModel : BaseViewModel
    {
        private readonly NewsRepository _newsRepository;
        private readonly Translator _translationService;
        public ObservableCollection<NewsModel> News { get; set; }

        public NewsViewModel()
        {
            _newsRepository = new NewsRepository();
            _translationService = new Translator(); // Make sure this line is being executed.
            News = new ObservableCollection<NewsModel>();

            // Загружаем новости при инициализации модели представления
            _ = LoadNewsAsync();
        }

        private async Task LoadNewsAsync()
        {
            var newsList = await _newsRepository.GetNewsAsync();
            News.Clear();
            foreach (var news in newsList)
            {
                // Переводим заголовок и описание
                string translatedTitle = await TranslateTextAsync(news.Title);
                string translatedDescription = await TranslateTextAsync(news.Description);

                // Логируем процесс перевода в консоль
                Console.WriteLine($"Original Title: {news.Title}");
                Console.WriteLine($"Translated Title: {translatedTitle}");
                Console.WriteLine($"Original Description: {news.Description}");
                Console.WriteLine($"Translated Description: {translatedDescription}");

                // Обновляем новость с переведенными значениями
                news.Title = translatedTitle;
                news.Description = translatedDescription;

                // Добавляем переведенные новости в ObservableCollection
                News.Add(news);
            }
        }


        private async Task<string> TranslateTextAsync(string text)
        {
            try
            {
                // Переводим текст с английского на украинский
                string translatedText = await _translationService.TranslateAsync(text, "uk");
                return translatedText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error translating text: {ex.Message}");
                return text; // Если не удалось перевести, возвращаем оригинальный текст
            }
        }


    }
}
