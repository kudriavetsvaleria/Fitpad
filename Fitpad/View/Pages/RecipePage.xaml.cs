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
            LoadRecipe();
        }

        private async void LoadRecipe()
        {
            var repository = new NutritionRepository();
            var details = await repository.GetRecipeDetailsAsync(_model.Id); // Создайте метод GetRecipeDetailsAsync
            _model.RecipeDetails = details;
            DataContext = _model;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
