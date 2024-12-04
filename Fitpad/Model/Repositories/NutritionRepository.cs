using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Fitpad.Model.Entities;
using System.Text.RegularExpressions;

namespace Fitpad.Model.Repositories
{
    public class NutritionRepository
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "77fc6d4be49f4522900362727af5549f";
        private const string BaseUrl = "https://api.spoonacular.com/recipes";

        public NutritionRepository()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Удаляет HTML-теги из строки.
        /// </summary>
        public string StripHtmlTags(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return Regex.Replace(input, "<.*?>", string.Empty).Trim();
        }

        /// <summary>
        /// Получает инструкции по рецепту по его ID.
        /// </summary>
        public async Task<string> GetRecipeDetailsAsync(int id)
        {
            var url = $"{BaseUrl}/{id}/information?apiKey={ApiKey}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Ошибка запроса: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            var recipe = JsonConvert.DeserializeObject<RecipeDetails>(json);

            return StripHtmlTags(recipe.Instructions ?? "Инструкции отсутствуют.");
        }

        /// <summary>
        /// Получает список рецептов, либо случайных, либо с заданным смещением.
        /// </summary>
        public async Task<List<NutritionModel>> GetRecipesAsync(bool useRandom = false, int offset = 0)
        {
            // Используем random=true, если указан флаг.
            var url = useRandom
                ? $"{BaseUrl}/random?number=48&apiKey={ApiKey}"
                : $"{BaseUrl}/complexSearch?number=48&offset={offset}&apiKey={ApiKey}&addRecipeInformation=true&addRecipeNutrition=true";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Ошибка запроса: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();

            if (useRandom)
            {
                var randomResponse = JsonConvert.DeserializeObject<RandomApiResponse>(json);
                return MapRandomRecipes(randomResponse.Recipes);
            }
            else
            {
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(json);
                return MapComplexSearchRecipes(apiResponse.Results);
            }
        }

        /// <summary>
        /// Преобразует результаты complexSearch в модели.
        /// </summary>
        private List<NutritionModel> MapComplexSearchRecipes(List<Recipe> recipes)
        {
            var result = new List<NutritionModel>();
            foreach (var recipe in recipes)
            {
                result.Add(new NutritionModel
                {
                    Id = recipe.Id,
                    Title = recipe.Title,
                    Image = recipe.Image,
                    Calories = (int)(recipe.Nutrition?.Nutrients?.Find(n => n.Name == "Calories")?.Amount ?? 0),
                    Protein = recipe.Nutrition?.Nutrients?.Find(n => n.Name == "Protein")?.Amount ?? 0,
                    Carbs = recipe.Nutrition?.Nutrients?.Find(n => n.Name == "Carbohydrates")?.Amount ?? 0,
                    ReadyInMinutes = recipe.ReadyInMinutes
                });
            }
            return result;
        }

        /// <summary>
        /// Преобразует результаты random в модели.
        /// </summary>
        private List<NutritionModel> MapRandomRecipes(List<Recipe> recipes)
        {
            var result = new List<NutritionModel>();
            foreach (var recipe in recipes)
            {
                result.Add(new NutritionModel
                {
                    Id = recipe.Id,
                    Title = recipe.Title,
                    Image = recipe.Image,
                    Calories = 0, // Random не возвращает Nutrition
                    Protein = 0,
                    Carbs = 0,
                    ReadyInMinutes = recipe.ReadyInMinutes
                });
            }
            return result;
        }

        public class ApiResponse
        {
            public List<Recipe> Results { get; set; }
        }

        public class RandomApiResponse
        {
            public List<Recipe> Recipes { get; set; }
        }

        public class Recipe
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Image { get; set; }
            public Nutrition Nutrition { get; set; }
            public int ReadyInMinutes { get; set; }
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

        public class RecipeDetails
        {
            public string Instructions { get; set; }
        }
    }
}
