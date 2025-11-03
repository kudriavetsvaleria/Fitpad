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

        private const string CacheCollection = "NewsCache";
        private const int CacheLifetimeMinutes = 120;

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

            News.Clear();
            foreach (var news in newsList)
                News.Add(news);
        }

        // ---- допоміжний валідатор url зображення
        private static bool HasValidImage(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (url.Equals("null", StringComparison.OrdinalIgnoreCase)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (!(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) return false;

            // іноді приходить about:blank або data: - ігноруємо
            if (uri.Scheme.Equals("about", StringComparison.OrdinalIgnoreCase)) return false;
            if (uri.Scheme.Equals("data", StringComparison.OrdinalIgnoreCase)) return false;

            return true;
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
                    // пропускаємо без картинки
                    if (!HasValidImage(news.UrlToImage)) continue;

                    var translatedTitle = await _translatorService.TranslateTextAsync(news.Title, "uk");
                    var translatedDescription = await _translatorService.TranslateTextAsync(news.Description, "uk");

                    translatedNews.Add(new NewsModel
                    {
                        Title = translatedTitle,
                        Description = translatedDescription,
                        UrlToImage = news.UrlToImage
                    });
                }

                await CacheNewsAsync(translatedNews);
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
                            if (newsArray == null || newsArray.Count == 0)
                                return new ObservableCollection<NewsModel>();

                            var validNews = newsArray
                                .Where(n => n != null)
                                .Select(n => new NewsModel
                                {
                                    Title = n.ContainsKey("Title") && n["Title"] != null ? n["Title"].ToString() : "Без заголовка",
                                    Description = n.ContainsKey("Description") && n["Description"] != null ? n["Description"].ToString() : "Опис відсутній",
                                    UrlToImage = n.ContainsKey("UrlToImage") && n["UrlToImage"] != null ? n["UrlToImage"].ToString() : string.Empty
                                })
                                // тут ще раз відфільтруємо без зображення
                                .Where(m => HasValidImage(m.UrlToImage))
                                .ToList();

                            return new ObservableCollection<NewsModel>(validNews);
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
