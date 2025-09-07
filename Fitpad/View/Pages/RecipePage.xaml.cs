using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Fitpad.Services;

namespace Fitpad.View.Pages
{
    public partial class RecipePage : Page
    {
        private readonly NutritionModel _model;
        private readonly TranslatorService _translator = new TranslatorService();

        public RecipePage(NutritionModel model)
        {
            InitializeComponent();
            _model = model ?? new NutritionModel();
            DataContext = _model;
            _ = LoadRecipeDetailsAsync();
        }

        private async Task LoadRecipeDetailsAsync()
        {
            var repo = new NutritionRepository();

            if (!int.TryParse(_model.Id, out var rid))
            {
                MessageBox.Show("Не вдалося розпізнати ID рецепта.", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (instructions, ingredients) = await repo.GetRecipeDetailsWithIngredientsAsync(rid);

            _model.RecipeDetails = await _translator.TranslateTextAsync(instructions);
            _model.Ingredients = new List<string>();

            foreach (var ing in ingredients)
            {
                _model.Ingredients.Add(await _translator.TranslateTextAsync(ing));
            }

            // Обновляем биндинги
            Dispatcher.Invoke(() =>
            {
                DataContext = null;
                DataContext = _model;
            });
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
    }
}
