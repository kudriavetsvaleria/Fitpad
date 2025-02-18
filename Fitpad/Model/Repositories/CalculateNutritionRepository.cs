using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Fitpad.Model.Entities;
using Fitpad.Services;
using Google.Cloud.Firestore;

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

        public async Task<List<NutritionModel>> GetUserProductsAsync(string userId)
        {
            List<NutritionModel> products = new List<NutritionModel>();

            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("❌ Помилка: UserID не знайдено.");
                    return products;
                }

                FirestoreDb db = FirestoreDb.Create("fitpad-2025");
                CollectionReference userProductsRef = db.Collection("Users").Document(userId).Collection("UserProducts");

                QuerySnapshot snapshot = await userProductsRef.GetSnapshotAsync();

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        NutritionModel product = doc.ConvertTo<NutritionModel>();
                        products.Add(product);
                    }
                }

                Console.WriteLine($"✅ Завантажено {products.Count} продуктів для користувача {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Помилка під час завантаження продуктів: {ex.Message}");
            }

            return products;
        }


        public async Task<List<NutritionModel>> GetProductsAsync(string query)
        {
            try
            {
                string translatedQuery = await _translator.TranslateTextAsync(query, "en");
                Console.WriteLine($"Перекладений запит: {translatedQuery}");

                string url = $"{BaseUrl}?search_terms={Uri.EscapeDataString(translatedQuery)}&search_simple=1&action=process&json=1";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Помилка запиту: {response.StatusCode}");
                    return new List<NutritionModel>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(json);
                var result = new List<NutritionModel>();

                if (apiResponse.Products != null && apiResponse.Products.Count > 0)
                {
                    var product = apiResponse.Products[0];
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
                    Console.WriteLine("⚠ УВАГА: API не знайшов продукти!");
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ Помилка HTTP: {ex.Message}");
                return new List<NutritionModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Загальна помилка: {ex.Message}");
                return new List<NutritionModel>();
            }
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
