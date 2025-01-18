using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class NewsViewModel : BaseViewModel
    {
        private readonly NewsRepository _newsRepository;
        public ObservableCollection<NewsModel> News { get; set; }

        public NewsViewModel()
        {
            _newsRepository = new NewsRepository();
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
                News.Add(news);
            }
        }
    }
}
