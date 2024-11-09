using Fitpad.View.Components;
using Fitpad.View.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class NewsPageViewModel
    {
        private ApiService _apiService;

        // Свойство для хранения списка новостей
        public List<NewsItem> NewsItems { get; set; }

        public NewsPageViewModel()
        {
            _apiService = new ApiService();
            NewsItems = new List<NewsItem>(); // Инициализируем список
        }

        // Метод для загрузки новостей
        public async Task LoadNewsAsync()
        {
            string url = "https://example.com/api/news"; // URL веб-портала
            var news = await _apiService.GetDataAsync<List<NewsItem>>(url); // Получаем данные

            if (news != null)
            {
                NewsItems = news; // Присваиваем полученные данные в свойство
            }
        }
    }
}
