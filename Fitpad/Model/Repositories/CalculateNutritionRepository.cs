using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Fitpad.Model.Entities;
using Fitpad.Services; // Подключаем твой TranslatorService

namespace Fitpad.Model.Repositories
{
    public class CalculateNutritionRepository
    {
        private readonly HttpClient _httpClient;
        private readonly TranslatorService _translator; // Подключаем переводчик
        private const string BaseUrl = "https://world.openfoodfacts.org/cgi/search.pl";

        public CalculateNutritionRepository()
        {
            _httpClient = new HttpClient();
            _translator = new TranslatorService();
        }

        public async Task<List<SavedProductModel>> GetProductsAsync(string query)
        {
            // Переводим запрос на английский
            string translatedQuery = await _translator.TranslateTextAsync(query, "en");
            Console.WriteLine($"Переведённый запрос: {translatedQuery}");

            string url = $"{BaseUrl}?search_terms={Uri.EscapeDataString(translatedQuery)}&search_simple=1&action=process&json=1";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка запроса: {response.StatusCode}");
                throw new HttpRequestException($"Ошибка запроса: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Ответ от OpenFoodFacts: {json}");

            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(json);
            var result = new List<SavedProductModel>();

            if (apiResponse.Products != null && apiResponse.Products.Count > 0)
            {
                var product = apiResponse.Products[0]; // Берём первый найденный продукт
                Console.WriteLine($"Найден продукт: {product.ProductName}");

                // Если `ProductName` пустой, логируем проблему
                if (string.IsNullOrEmpty(product.ProductName))
                {
                    Console.WriteLine("⚠ ВНИМАНИЕ: API не вернул название продукта!");
                }

                // Переводим название обратно на украинский, если оно есть
                string translatedTitle = string.IsNullOrEmpty(product.ProductName)
                    ? "Невідомий продукт"
                    : await _translator.TranslateTextAsync(product.ProductName, "uk");

                var savedProduct = new SavedProductModel
                {
                    Id = int.TryParse(product.Code, out int id) ? id : 0,
                    Name = translatedTitle,
                    Title = translatedTitle,
                    Image = product.ImageUrl ?? "",
                    Calories = (int)(product.Nutriments?.EnergyKcal ?? 0),
                    Protein = product.Nutriments?.Proteins ?? 0,
                    Carbs = product.Nutriments?.Carbohydrates ?? 0,
                    Fats = product.Nutriments?.Fats ?? 0,
                    Water = product.Nutriments?.Water ?? 0,
                    Weight = 100,
                    Time = DateTime.Now.ToString("HH:mm")
                };

                if (!string.IsNullOrEmpty(savedProduct.Name) && (savedProduct.Calories > 0 || savedProduct.Protein > 0 || savedProduct.Carbs > 0 || savedProduct.Fats > 0))
                {
                    result.Add(savedProduct);
                }
            }
            else
            {
                Console.WriteLine("⚠ ВНИМАНИЕ: API не нашёл продукты!");
            }

            return result;
        }


        private class ApiResponse
        {
            public List<Product> Products { get; set; }
        }

        private class Product
        {
            public string Code { get; set; }
            public string ProductName { get; set; }
            public string ImageUrl { get; set; }
            public Nutriments Nutriments { get; set; }
        }

        private class Nutriments
        {
            [JsonProperty("energy-kcal")]
            public double EnergyKcal { get; set; }

            [JsonProperty("proteins")]
            public double Proteins { get; set; }

            [JsonProperty("carbohydrates")]
            public double Carbohydrates { get; set; }

            [JsonProperty("fat")]
            public double Fats { get; set; }

            [JsonProperty("water")]
            public double Water { get; set; }
        }
    }
}
