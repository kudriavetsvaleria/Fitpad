using System;
using System.Windows;
using System.Windows.Controls;
using Fitpad.Model.Entities;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad.View.Pages
{
    public partial class NutritionPage : Page
    {
        private readonly NutritionViewModel _viewModel;

        private void Card_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is NutritionModel model)
            {
                // Переход на страницу рецепта
                NavigationService.Navigate(new RecipePage(model));
            }
        }

        public NutritionPage()
        {
            InitializeComponent();

            _viewModel = new NutritionViewModel();
            DataContext = _viewModel;

            Loaded += async (s, e) =>
            {
                var random = new Random();
                int offset = random.Next(0, 1000); // Диапазон смещения
                await _viewModel.LoadNutritionAsync(false, offset);

            };

        }
    }
}
