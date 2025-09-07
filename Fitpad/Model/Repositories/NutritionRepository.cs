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
        private static readonly HttpClient _http = new HttpClient();

        // TODO: хранить ключ не в коде
        private const string ApiKey = "86241b5ba83247a39ebe1362a765a007";
        private const string BaseUrl = "https://api.spoonacular.com/recipes";

        public string StripHtmlTags(string input) =>
            string.IsNullOrWhiteSpace(input) ? string.Empty : Regex.Replace(input, "<.*?>", string.Empty).Trim();

        public async Task<NutritionModel> GetRecipeDetailsAsync(int recipeId)
        {
            var url = $"{BaseUrl}/{recipeId}/information?apiKey={ApiKey}&includeNutrition=true";
            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"Ошибка запроса: {resp.StatusCode}");

            var json = await resp.Content.ReadAsStringAsync();
            var r = JsonConvert.DeserializeObject<Recipe>(json);

            return new NutritionModel
            {
                Id = r.Id.ToString(),
                Title = r.Title,
                Image = r.Image,
                Calories = r.Nutrition?.Nutrients?.Find(n => n.Name == "Calories")?.Amount ?? 0,
                Protein = r.Nutrition?.Nutrients?.Find(n => n.Name == "Protein")?.Amount ?? 0,
                Carbs = r.Nutrition?.Nutrients?.Find(n => n.Name == "Carbohydrates")?.Amount ?? 0,
                Fats = r.Nutrition?.Nutrients?.Find(n => n.Name == "Fat")?.Amount ?? 0,
                ReadyInMinutes = r.ReadyInMinutes
            };
        }

        public async Task<(string Instructions, List<string> Ingredients)> GetRecipeDetailsWithIngredientsAsync(int id)
        {
            var url = $"{BaseUrl}/{id}/information?apiKey={ApiKey}";
            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"Помилка запиту: {resp.StatusCode}");

            var json = await resp.Content.ReadAsStringAsync();
            var r = JsonConvert.DeserializeObject<RecipeDetailsResponse>(json);

            var ingredients = new List<string>();
            if (r?.ExtendedIngredients != null)
                foreach (var i in r.ExtendedIngredients) ingredients.Add(i.Original);

            return (StripHtmlTags(r?.Instructions ?? "Інструкції не знайдено."), ingredients);
        }

        public async Task<List<NutritionModel>> SearchRecipesAsync(string query)
        {
            var url = $"{BaseUrl}/complexSearch?query={Uri.EscapeDataString(query ?? "")}&apiKey={ApiKey}&addRecipeInformation=true";
            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"Ошибка запроса: {resp.StatusCode}");

            var json = await resp.Content.ReadAsStringAsync();
            var api = JsonConvert.DeserializeObject<ApiResponse>(json);

            var list = new List<NutritionModel>();
            if (api?.Results == null) return list;

            foreach (var r in api.Results)
            {
                list.Add(new NutritionModel
                {
                    Id = r.Id.ToString(),
                    Title = r.Title,
                    Image = r.Image,
                    Calories = r.Nutrition?.Nutrients?.Find(n => n.Name == "Calories")?.Amount ?? 0,
                    Protein = r.Nutrition?.Nutrients?.Find(n => n.Name == "Protein")?.Amount ?? 0,
                    Carbs = r.Nutrition?.Nutrients?.Find(n => n.Name == "Carbohydrates")?.Amount ?? 0,
                    Fats = r.Nutrition?.Nutrients?.Find(n => n.Name == "Fat")?.Amount ?? 0,
                    ReadyInMinutes = r.ReadyInMinutes
                });
            }
            return list;
        }

        public async Task<List<NutritionModel>> GetRecipesAsync(bool useRandom = false, int offset = 0)
        {
            var url = useRandom
                ? $"{BaseUrl}/random?number=48&apiKey={ApiKey}"
                : $"{BaseUrl}/complexSearch?number=24&offset={offset}&apiKey={ApiKey}&addRecipeInformation=true&addRecipeNutrition=true";

            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"Помилка запиту: {resp.StatusCode}");

            var json = await resp.Content.ReadAsStringAsync();
            var api = JsonConvert.DeserializeObject<ApiResponse>(json);

            var list = new List<NutritionModel>();
            if (api?.Results == null) return list;

            foreach (var r in api.Results)
            {
                list.Add(new NutritionModel
                {
                    Id = r.Id.ToString(),
                    Title = r.Title,
                    Image = r.Image,
                    Calories = r.Nutrition?.Nutrients?.Find(n => n.Name == "Calories")?.Amount ?? 0,
                    Protein = r.Nutrition?.Nutrients?.Find(n => n.Name == "Protein")?.Amount ?? 0,
                    Carbs = r.Nutrition?.Nutrients?.Find(n => n.Name == "Carbohydrates")?.Amount ?? 0,
                    Fats = r.Nutrition?.Nutrients?.Find(n => n.Name == "Fat")?.Amount ?? 0,
                    ReadyInMinutes = r.ReadyInMinutes
                });
            }
            return list;
        }

        // --- DTOs
        public class ApiResponse { public List<Recipe> Results { get; set; } }
        public class Recipe
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Image { get; set; }
            public Nutrition Nutrition { get; set; }
            public int ReadyInMinutes { get; set; }
        }
        public class Nutrition { public List<Nutrient> Nutrients { get; set; } }
        public class Nutrient { public string Name { get; set; } public double Amount { get; set; } }

        public class RecipeDetailsResponse
        {
            public string Instructions { get; set; }
            public List<Ingredient> ExtendedIngredients { get; set; }
        }
        public class Ingredient { public string Original { get; set; } }
    }
}
