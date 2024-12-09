using System.Windows;
using System.Windows.Controls;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;

namespace Fitpad.View.Pages
{
    public partial class RecipePage : Page
    {
        private readonly NutritionModel _model;

        public RecipePage(NutritionModel model)
        {
            InitializeComponent();
            _model = model;
            DataContext = _model; // Устанавливаем контекст данных
            LoadRecipe();
        }

        private async void LoadRecipe()
        {
            var repository = new NutritionRepository();
            var (instructions, ingredients) = await repository.GetRecipeDetailsWithIngredientsAsync(_model.Id);

            _model.RecipeDetails = instructions;
            _model.Ingredients = ingredients;

            DataContext = null; // Обновляем привязку
            DataContext = _model;
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack(); // Возврат на предыдущую страницу
        }
    }
}
