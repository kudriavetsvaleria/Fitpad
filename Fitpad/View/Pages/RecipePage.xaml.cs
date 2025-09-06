using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Fitpad.Services; // Добавьте это пространство имен, если еще не добавлено

namespace Fitpad.View.Pages
{
    public partial class RecipePage : Page
    {
        private readonly NutritionModel _model;
        private readonly TranslatorService _translator; // Добавлено поле для переводчика

        public RecipePage(NutritionModel model)
        {
            InitializeComponent();
            _model = model;
            _translator = new TranslatorService(); // Инициализация переводчика
            DataContext = _model; // Базовая информация доступна сразу
            _ = LoadRecipeDetailsAsync(); // Асинхронная загрузка деталей
        }

        private async Task LoadRecipeDetailsAsync()
        {
            var repository = new NutritionRepository();

            if (!int.TryParse(_model.Id, out var rid))
            {
                MessageBox.Show("Не удалось распознать ID рецепта.", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (instructions, ingredients) = await repository.GetRecipeDetailsWithIngredientsAsync(rid);

            _model.RecipeDetails = await _translator.TranslateTextAsync(instructions);
            _model.Ingredients = new List<string>();

            foreach (var ingredient in ingredients)
            {
                var translatedIngredient = await _translator.TranslateTextAsync(ingredient);
                _model.Ingredients.Add(translatedIngredient);
            }

            Dispatcher.Invoke(() =>
            {
                DataContext = null;
                DataContext = _model;
            });
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack(); // Возврат на предыдущую страницу
        }
    }
}
