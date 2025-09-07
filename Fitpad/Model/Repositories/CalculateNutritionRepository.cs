using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Fitpad.Model.Entities;

namespace Fitpad.Model.Repositories
{
    /// <summary>
    /// Берём продукты из OpenFoodFacts (данные на 100 г).
    /// ВАЖНО: сюда лучше передавать уже переведённый на EN query.
    /// </summary>
    public class CalculateNutritionRepository
    {
        private static readonly HttpClient _http = new HttpClient(); // единый экземпляр
        private const string BaseUrl = "https://world.openfoodfacts.org/cgi/search.pl";

        public async Task<List<NutritionModel>> GetProductsAsync(string query)
        {
            var list = new List<NutritionModel>();
            try
            {
                var url = $"{BaseUrl}?search_terms={Uri.EscapeDataString(query ?? "")}&search_simple=1&action=process&json=1";
                using var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return list;

                var json = await resp.Content.ReadAsStringAsync();
                var api = JsonConvert.DeserializeObject<ApiResponse>(json);

                if (api?.Products == null || api.Products.Count == 0) return list;

                var p = api.Products[0];
                var name = string.IsNullOrWhiteSpace(p.ProductName) ? query : p.ProductName;

                list.Add(new NutritionModel
                {
                    Id = !string.IsNullOrWhiteSpace(p.Code) ? p.Code : Guid.NewGuid().ToString(),
                    Name = name,
                    Title = name,
                    Image = p.ImageUrl ?? "",
                    Calories = p.Nutriments?.EnergyKcal ?? 0,
                    Protein = p.Nutriments?.Proteins ?? 0,
                    Carbs = p.Nutriments?.Carbohydrates ?? 0,
                    Fats = p.Nutriments?.Fats ?? 0,
                    Sugar = p.Nutriments?.Sugars ?? 0,
                    Water = p.Nutriments?.Water ?? 0,
                    Weight = 100,
                    Time = DateTime.Now.ToString("HH:mm")
                });

                return list;
            }
            catch
            {
                return list;
            }
        }

        private class ApiResponse { public List<Product> Products { get; set; } }

        private class Product
        {
            public string Code { get; set; }
            public string ProductName { get; set; }
            public string ImageUrl { get; set; }
            public Nutriments Nutriments { get; set; }
        }

        private class Nutriments
        {
            [JsonProperty("energy-kcal")] public double EnergyKcal { get; set; }
            [JsonProperty("proteins")] public double Proteins { get; set; }
            [JsonProperty("carbohydrates")] public double Carbohydrates { get; set; }
            [JsonProperty("fat")] public double Fats { get; set; }
            [JsonProperty("sugars")] public double Sugars { get; set; }
            [JsonProperty("water")] public double Water { get; set; }
        }
    }
}
