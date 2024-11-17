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
        private readonly string _apiKey = "6be473200c65428498902906f4d6f1b4"; // Проверь, что API ключ корректен
        private readonly string _baseUrl = "https://newsapi.org/v2/top-headlines";

        public async Task<List<NewsModel>> GetNewsAsync()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    // Формируем URL запроса
                    var url = $"{_baseUrl}?country={Uri.EscapeDataString("us")}&category={Uri.EscapeDataString("sports")}&apiKey={Uri.EscapeDataString(_apiKey)}";

                    // Настраиваем заголовки
                    httpClient.DefaultRequestHeaders.ConnectionClose = true;
                    httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

                    // Добавляем заголовок User-Agent
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "FitpadApp/1.0");

                    // Создаем HttpRequestMessage с использованием построенного URL
                    var request = new HttpRequestMessage(HttpMethod.Get, url);

                    // Выполняем запрос и проверяем ответ
                    var response = await httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error fetching news: {response.StatusCode}, {errorContent}");
                        return new List<NewsModel>();
                    }

                    // Чтение и десериализация JSON-ответа
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<NewsApiResponse>(jsonResponse);
                    return result.Articles;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occurred: {ex.Message}");
                    return new List<NewsModel>();
                }
            }
        }
    }

    public class NewsApiResponse
    {
        public List<NewsModel> Articles { get; set; }
    }
}
