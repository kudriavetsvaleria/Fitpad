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
            var details = await repository.GetRecipeDetailsAsync(_model.Id); // Получаем инструкции
            _model.RecipeDetails = details;

            // Обновляем контекст данных
            DataContext = null;
            DataContext = _model;

            // Отладка
            System.Diagnostics.Debug.WriteLine($"RecipeDetails: {_model.RecipeDetails}");
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack(); // Возврат на предыдущую страницу
        }
    }
}
