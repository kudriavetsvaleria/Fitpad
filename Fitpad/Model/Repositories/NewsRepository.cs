using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fitpad.Model.Entities;

namespace Fitpad.Model.Repositories
{
    public class NewsRepository
    {
        //private readonly string _apiKey = "ТВОЙ_API_KEY";
        //private readonly string _baseUrl = "https://newsapi.org/v2/top-headlines?country=us&category=sports";

        public async Task<List<NewsModel>> GetNewsAsync()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync($"https://newsapi.org/v2/top-headlines?category=sports&apiKey=6be473200c65428498902906f4d6f1b4");
                    var result = JsonConvert.DeserializeObject<NewsApiResponse>(response);
                    return result.Articles;
                }
                catch (Exception ex)
                {
                    // Обработка ошибок
                    Console.WriteLine($"Error fetching news: {ex.Message}");
                    return new List<NewsModel>();
                }
            }
        }
    }

    // Модель для десериализации JSON-ответа от API
    public class NewsApiResponse
    {
        public List<NewsModel> Articles { get; set; }
    }
}
