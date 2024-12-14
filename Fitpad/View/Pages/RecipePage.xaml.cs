using System.Threading.Tasks;
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
            DataContext = _model; // Базовая информация доступна сразу
            _ = LoadRecipeDetailsAsync(); // Асинхронная загрузка деталей
        }

        private async Task LoadRecipeDetailsAsync()
        {
            var repository = new NutritionRepository();
            var (instructions, ingredients) = await repository.GetRecipeDetailsWithIngredientsAsync(_model.Id);

            _model.RecipeDetails = instructions;
            _model.Ingredients = ingredients;

            // Обновляем привязку для отображения данных
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
