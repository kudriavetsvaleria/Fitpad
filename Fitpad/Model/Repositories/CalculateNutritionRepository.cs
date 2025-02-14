using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Fitpad.Model.Entities;
using Fitpad.Services;

namespace Fitpad.Model.Repositories
{
    public class CalculateNutritionRepository
    {
        private readonly HttpClient _httpClient;
        private readonly TranslatorService _translator;
        private const string BaseUrl = "https://world.openfoodfacts.org/cgi/search.pl";

        public CalculateNutritionRepository()
        {
            _httpClient = new HttpClient();
            _translator = new TranslatorService();
        }

        public async Task<List<NutritionModel>> GetProductsAsync(string query)
        {
            // Переводим запрос на английский перед отправкой
            string translatedQuery = await _translator.TranslateTextAsync(query, "en");
            Console.WriteLine($"Переведённый запрос: {translatedQuery}");

            string url = $"{BaseUrl}?search_terms={Uri.EscapeDataString(translatedQuery)}&search_simple=1&action=process&json=1";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ошибка запроса: {response.StatusCode}");
                return new List<NutritionModel>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(json);
            var result = new List<NutritionModel>();

            if (apiResponse.Products != null && apiResponse.Products.Count > 0)
            {
                var product = apiResponse.Products[0]; // Берем первый найденный продукт

                // Используем название, если его вернул API, иначе — оригинальный запрос
                string productName = string.IsNullOrEmpty(product.ProductName) ? query : product.ProductName;

                var savedProduct = new NutritionModel
                {
                    Id = int.TryParse(product.Code, out int id) ? id : 0,
                    Name = productName,
                    Title = productName,
                    Image = product.ImageUrl ?? "",
                    Calories = (int)(product.Nutriments?.EnergyKcal ?? 0),
                    Protein = product.Nutriments?.Proteins ?? 0,
                    Carbs = product.Nutriments?.Carbohydrates ?? 0,
                    Fats = product.Nutriments?.Fats ?? 0,
                    Water = product.Nutriments?.Water ?? 0,
                    Weight = 100,
                    Time = DateTime.Now.ToString("HH:mm")
                };

                result.Add(savedProduct);
            }
            else
            {
                Console.WriteLine("⚠ ВНИМАНИЕ: API не нашел продукты!");
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
