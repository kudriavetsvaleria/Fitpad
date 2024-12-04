using System.Windows;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using System.Windows.Controls;

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
            var details = await repository.GetRecipeDetailsAsync(_model.Id);
            _model.RecipeDetails = details;
            DataContext = _model;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
