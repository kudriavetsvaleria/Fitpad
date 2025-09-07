using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Fitpad.Services;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class NutritionViewModel : INotifyPropertyChanged
    {
        private readonly NutritionRepository _repository = new NutritionRepository();
        private readonly TranslatorService _translator = new TranslatorService();

        // Кэш переводов (простая LRU по размеру)
        private readonly Dictionary<string, string> _translationCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private const int CacheSizeLimit = 1000;

        private ObservableCollection<NutritionModel> _nutritionCards = new ObservableCollection<NutritionModel>();
        public ObservableCollection<NutritionModel> NutritionCards
        {
            get => _nutritionCards;
            set { _nutritionCards = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNutritionEmpty)); }
        }

        public bool IsNutritionEmpty => NutritionCards == null || NutritionCards.Count == 0;

        private bool _isSearchEmpty;
        public bool IsSearchEmpty
        {
            get => _isSearchEmpty;
            set { _isSearchEmpty = value; OnPropertyChanged(); }
        }

        private void AddToCache(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (_translationCache.Count >= CacheSizeLimit)
            {
                // наивное удаление самого старого по ключу
                var first = _translationCache.Keys.FirstOrDefault();
                if (first != null) _translationCache.Remove(first);
            }
            _translationCache[key] = value ?? string.Empty;
        }

        private async Task<string> TranslateCachedAsync(string text, string to = "uk")
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            if (_translationCache.TryGetValue($"{to}:{text}", out var cached)) return cached;

            var translated = await _translator.TranslateTextAsync(text, to);
            AddToCache($"{to}:{text}", translated);
            return translated;
        }

        public async Task TranslateNutritionCardsAsync()
        {
            // Переводим карточки «на месте»
            var tasks = NutritionCards.Select(async card =>
            {
                card.Title = await TranslateCachedAsync(card.Title);
                card.RecipeDetails = await TranslateCachedAsync(card.RecipeDetails);
                return 0;
            });

            await Task.WhenAll(tasks);
            OnPropertyChanged(nameof(NutritionCards));
        }

        public async Task LoadNutritionAsync(bool useRandom, int offset)
        {
            NutritionCards.Clear();

            var recipes = await _repository.GetRecipesAsync(useRandom, offset);

            // Переводим параллельно (название + описание)
            var translated = await Task.WhenAll(recipes.Select(async r =>
            {
                r.Title = await TranslateCachedAsync(r.Title);
                r.RecipeDetails = await TranslateCachedAsync(r.RecipeDetails);
                return r;
            }));

            foreach (var recipe in translated)
                NutritionCards.Add(recipe);

            OnPropertyChanged(nameof(NutritionCards));
        }

        public async Task LoadMoreNutritionAsync(int offset)
        {
            var recipes = await _repository.GetRecipesAsync(false, offset);
            foreach (var recipe in recipes)
                NutritionCards.Add(recipe);
        }

        public async Task SearchNutritionAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                NutritionCards.Clear();
                IsSearchEmpty = true;
                return;
            }

            // Переводим поисковую строку на EN для API
            var translatedQuery = await TranslateCachedAsync(query, "en");

            NutritionCards.Clear();

            var recipes = await _repository.SearchRecipesAsync(translatedQuery);

            // Загружаем подробности параллельно, потом переводим на украинский для UI
            var detailed = await Task.WhenAll(recipes.Select(async recipe =>
            {
                if (int.TryParse(recipe.Id, out var rid))
                {
                    var detailedRecipe = await _repository.GetRecipeDetailsAsync(rid);
                    recipe.Calories = detailedRecipe.Calories;
                    recipe.Protein = detailedRecipe.Protein;
                    recipe.Carbs = detailedRecipe.Carbs;
                    recipe.Fats = detailedRecipe.Fats;
                    recipe.RecipeDetails = detailedRecipe.RecipeDetails;
                    recipe.Ingredients = detailedRecipe.Ingredients;
                }

                recipe.Title = await TranslateCachedAsync(recipe.Title);
                recipe.RecipeDetails = await TranslateCachedAsync(recipe.RecipeDetails);
                return recipe;
            }));

            foreach (var r in detailed)
                NutritionCards.Add(r);

            IsSearchEmpty = NutritionCards.Count == 0;
            OnPropertyChanged(nameof(IsSearchEmpty));
            OnPropertyChanged(nameof(NutritionCards));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
