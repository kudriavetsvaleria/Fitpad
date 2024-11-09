using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fitpad.View.Repositories // или Fitpad.View.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Таймаут для запроса
        }

        // Асинхронный метод для отправки GET-запроса
        public async Task<T> GetDataAsync<T>(string url)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    T data = JsonConvert.DeserializeObject<T>(json); // Десериализация JSON
                    return data;
                }
                else
                {
                    throw new Exception("Ошибка при получении данных.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return default; // Возвращаем default значение для типа T в случае ошибки
            }
        }

        // Другие методы, например, для POST-запросов, можно добавить по аналогии
    }
}
