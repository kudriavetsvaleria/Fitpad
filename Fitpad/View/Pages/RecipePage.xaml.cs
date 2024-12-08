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
            var details = await repository.GetRecipeDetailsAsync(_model.Id); // Загружаем детали
            _model.RecipeDetails = details; // Инструкции без HTML тегов
            DataContext = null; // Обновляем привязку
            DataContext = _model;
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack(); // Возврат на предыдущую страницу
        }
    }
}
