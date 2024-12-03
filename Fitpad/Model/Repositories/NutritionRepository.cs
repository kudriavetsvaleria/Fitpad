using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Fitpad.Model.Entities;

namespace Fitpad.Model.Repositories
{
    public class NutritionRepository
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "77fc6d4be49f4522900362727af5549f"; // Ваш API-ключ
        private const string BaseUrl = "https://api.spoonacular.com/recipes";

        public NutritionRepository()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<NutritionModel>> GetRecipesAsync()
        {
            var url = $"{BaseUrl}/complexSearch?number=10&apiKey={ApiKey}&addRecipeInformation=true&addRecipeNutrition=true";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Ошибка запроса: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(json);

            var result = new List<NutritionModel>();
            foreach (var recipe in apiResponse.Results)
            {
                var calories = 0;
                var protein = 0.0;
                var carbs = 0.0;

                if (recipe.Nutrition?.Nutrients != null)
                {
                    calories = (int)(recipe.Nutrition.Nutrients.Find(n => n.Name == "Calories")?.Amount ?? 0);
                    protein = recipe.Nutrition.Nutrients.Find(n => n.Name == "Protein")?.Amount ?? 0;
                    carbs = recipe.Nutrition.Nutrients.Find(n => n.Name == "Carbohydrates")?.Amount ?? 0;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Рецепт {recipe.Title} не содержит Nutrition. Используются значения по умолчанию.");
                }

                result.Add(new NutritionModel
                {
                    Title = recipe.Title,
                    Image = recipe.Image,
                    Calories = calories,
                    Protein = protein,
                    Carbs = carbs,
                    ReadyInMinutes = recipe.ReadyInMinutes // Время приготовления
                });
            }

            return result;
        }

        // Модели для парсинга ответа API
        public class ApiResponse
        {
            public List<Recipe> Results { get; set; }
        }

        public class Recipe
        {
            public string Title { get; set; }
            public string Image { get; set; }
            public Nutrition Nutrition { get; set; }
            public int ReadyInMinutes { get; set; } // Время приготовления
        }

        public class Nutrition
        {
            public List<Nutrient> Nutrients { get; set; }
        }

        public class Nutrient
        {
            public string Name { get; set; }
            public double Amount { get; set; }
        }
    }
}
