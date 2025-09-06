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
using Fitpad.Model.Entities;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class NutritionViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<NutritionModel> _nutritionCards;
        private readonly TranslatorService _translator;
        private readonly Dictionary<string, string> _translationCache = new Dictionary<string, string>(); // Кэш для переводов
        private const int CacheSizeLimit = 1000;
        public ObservableCollection<NutritionModel> NutritionCards
        {
            get => _nutritionCards;
            set
            {
                _nutritionCards = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNutritionEmpty)); // Обновляем состояние пустоты
            }
        }
        public bool IsNutritionEmpty => NutritionCards == null || NutritionCards.Count == 0; // Если список пуст
        private readonly NutritionRepository _repository;

        public NutritionViewModel()
        {
            _repository = new NutritionRepository();
            _translator = new TranslatorService(); // Инициализация переводчика
            NutritionCards = new ObservableCollection<NutritionModel>();
        }

        private void AddToCache(string query, string translatedQuery)
        {
            if (_translationCache.Count >= CacheSizeLimit)
            {
                var firstKey = _translationCache.Keys.First();
                _translationCache.Remove(firstKey);
                Console.WriteLine($"Удален из кэша: {firstKey}");
            }
            _translationCache[query] = translatedQuery;
        }

        public async Task TranslateNutritionCardsAsync()
        {
            foreach (var card in NutritionCards)
            {
                card.Title = await _translator.TranslateTextAsync(card.Title);
                card.RecipeDetails = await _translator.TranslateTextAsync(card.RecipeDetails);
            }
            OnPropertyChanged(nameof(NutritionCards)); // Обновляем привязку
        }

        public async Task LoadNutritionAsync(bool useRandom, int offset)
        {
            Console.WriteLine("Начинается загрузка рецептов...");

            NutritionCards.Clear();
            var recipes = await _repository.GetRecipesAsync(useRandom, offset);

            foreach (var recipe in recipes)
            {
                Console.WriteLine($"Переводим рецепт: {recipe.Title}");
                recipe.Title = await _translator.TranslateTextAsync(recipe.Title);
                recipe.RecipeDetails = await _translator.TranslateTextAsync(recipe.RecipeDetails);
                NutritionCards.Add(recipe);
            }

            OnPropertyChanged(nameof(NutritionCards));
            Console.WriteLine("Загрузка и перевод завершены.");
        }



        public async Task LoadMoreNutritionAsync(int offset)
        {
            var recipes = await _repository.GetRecipesAsync(false, offset);
            foreach (var recipe in recipes)
            {
                NutritionCards.Add(recipe);
            }
        }

        private bool _isSearchEmpty;
        public bool IsSearchEmpty
        {
            get => _isSearchEmpty;
            set
            {
                _isSearchEmpty = value;
                OnPropertyChanged(); // Уведомляем привязку об изменении
            }
        }

        public async Task SearchNutritionAsync(string query)
        {
            // Проверяем наличие запроса в кэше
            if (!_translationCache.TryGetValue(query, out var translatedQuery))
            {
                translatedQuery = await _translator.TranslateTextAsync(query, "en");
                _translationCache[query] = translatedQuery;
                Console.WriteLine($"Добавлено в кэш: {query} -> {translatedQuery}");
            }
            else
            {
                Console.WriteLine($"Использован кэш: {query} -> {translatedQuery}");
            }

            NutritionCards.Clear(); // Очистка текущего списка

            var recipes = await _repository.SearchRecipesAsync(translatedQuery);

            // Запрашиваем детали рецептов параллельно
            // Запрашиваем детали рецептов параллельно
            var tasks = recipes.Select(async recipe =>
            {
                // преобразуем string → int
                if (int.TryParse(recipe.Id, out int recipeId))
                {
                    var detailedRecipe = await _repository.GetRecipeDetailsAsync(recipeId);
                    recipe.Calories = detailedRecipe.Calories;
                    recipe.Protein = detailedRecipe.Protein;
                    recipe.Carbs = detailedRecipe.Carbs;
                    recipe.Fats = detailedRecipe.Fats;
                    recipe.RecipeDetails = detailedRecipe.RecipeDetails;
                    recipe.Ingredients = detailedRecipe.Ingredients;
                }
                else
                {
                    Console.WriteLine($"⚠ Не удалось преобразовать Id рецепта '{recipe.Id}' в число!");
                }

                // Переводим название и описание рецепта
                recipe.Title = await _translator.TranslateTextAsync(recipe.Title);
                recipe.RecipeDetails = await _translator.TranslateTextAsync(recipe.RecipeDetails);

                return recipe;
            });


            var detailedRecipes = await Task.WhenAll(tasks); // Ожидаем завершения всех запросов

            foreach (var recipe in detailedRecipes)
            {
                NutritionCards.Add(recipe); // Добавляем рецепты в список
            }

            IsSearchEmpty = NutritionCards.Count == 0;
            OnPropertyChanged(nameof(IsSearchEmpty));
            OnPropertyChanged(nameof(NutritionCards)); // Обновляем привязку для отображения данных
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
