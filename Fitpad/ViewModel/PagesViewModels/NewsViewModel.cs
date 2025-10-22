using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Fitpad.Services;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class NewsViewModel : BaseViewModel
    {
        private readonly NewsRepository _newsRepository;
        private readonly TranslatorService _translatorService;
        private readonly FirestoreDb _firestoreDb;
        public ObservableCollection<NewsModel> News { get; set; }

        private const string CacheCollection = "NewsCache"; // Коллекция в Firestore
        private const int CacheLifetimeMinutes = 120; // Время жизни кэша (2 часа)

        public NewsViewModel()
        {
            _newsRepository = new NewsRepository();
            _translatorService = new TranslatorService();
            _firestoreDb = FirestoreDb.Create("fitpad-2025");
            News = new ObservableCollection<NewsModel>();

            _ = LoadNewsAsync();
        }

        private async Task LoadNewsAsync()
        {
            var newsList = await GetCachedOrTranslatedNewsAsync();

            // Заполняем коллекцию без перезаписи ссылки
            News.Clear();
            foreach (var news in newsList)
            {
                News.Add(news);
            }
        }

        private async Task<ObservableCollection<NewsModel>> GetCachedOrTranslatedNewsAsync()
        {
            try
            {
                var cachedNews = await GetCachedNewsAsync();

                if (cachedNews.Count > 0)
                {
                    Console.WriteLine("Новости загружены из кэша.");
                    return cachedNews;
                }

                Console.WriteLine("⚡ Новости не найдены в кэше. Загружаем и переводим...");
                var newsList = await _newsRepository.GetNewsAsync();

                var translatedNews = new ObservableCollection<NewsModel>();

                foreach (var news in newsList)
                {
                    var translatedTitle = await _translatorService.TranslateTextAsync(news.Title, "uk");
                    var translatedDescription = await _translatorService.TranslateTextAsync(news.Description, "uk");

                    translatedNews.Add(new NewsModel
                    {
                        Title = translatedTitle,
                        Description = translatedDescription,
                        UrlToImage = news.UrlToImage
                    });
                }

                await CacheNewsAsync(translatedNews); // Передаем ObservableCollection

                return translatedNews;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке новостей: {ex.Message}");
                return new ObservableCollection<NewsModel>();
            }
        }

        private async Task CacheNewsAsync(ObservableCollection<NewsModel> newsList)
        {
            var collectionRef = _firestoreDb.Collection(CacheCollection);
            await collectionRef.Document("LatestNews").SetAsync(new
            {
                LastUpdated = DateTime.UtcNow,
                News = newsList.Select(n => new
                {
                    n.Title,
                    n.Description,
                    n.UrlToImage
                }).ToList()
            });

            Console.WriteLine("Новости закешированы в Firestore");
        }

        private async Task<ObservableCollection<NewsModel>> GetCachedNewsAsync()
        {
            try
            {
                var docRef = _firestoreDb.Collection(CacheCollection).Document("LatestNews");
                var snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    var lastUpdated = snapshot.GetValue<DateTime>("LastUpdated");

                    if ((DateTime.UtcNow - lastUpdated).TotalMinutes < CacheLifetimeMinutes)
                    {
                        Console.WriteLine("Загружаем новости из кэша...");

                        if (snapshot.ContainsField("News"))
                        {
                            var newsArray = snapshot.GetValue<List<Dictionary<string, object>>>("News");

                            // Проверяем, что newsArray не null
                            if (newsArray == null || newsArray.Count == 0)
                            {
                                Console.WriteLine("Кэшированные новости пусты или отсутствуют.");
                                return new ObservableCollection<NewsModel>();
                            }

                            // Фильтруем null-значения перед обработкой
                            var validNews = newsArray.Where(n => n != null).ToList();

                            var cachedNews = new ObservableCollection<NewsModel>(
                                validNews.Select(n => new NewsModel
                                {
                                    Title = n.ContainsKey("Title") && n["Title"] != null ? n["Title"].ToString() : "Без заголовка",
                                    Description = n.ContainsKey("Description") && n["Description"] != null ? n["Description"].ToString() : "Опис відсутній",
                                    UrlToImage = n.ContainsKey("UrlToImage") && n["UrlToImage"] != null ? n["UrlToImage"].ToString() : string.Empty
                                }).ToList());

                            return cachedNews;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении кэшированных новостей: {ex.Message}");
            }

            return new ObservableCollection<NewsModel>();
        }


    }
}
