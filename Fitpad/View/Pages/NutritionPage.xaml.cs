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

        public NutritionPage()
        {
            InitializeComponent();

            _viewModel = new NutritionViewModel();
            DataContext = _viewModel;

            // Не обновляем данные автоматически, чтобы избежать повторной загрузки
            if (_viewModel.NutritionCards.Count == 0)
            {
                var random = new Random();
                int offset = random.Next(0, 1000); // Диапазон для смещения
                _viewModel.LoadNutritionAsync(false, offset);
            }

        }


        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            var random = new Random();
            int offset = random.Next(0, 1000); // Новый диапазон для обновления
            await _viewModel.LoadNutritionAsync(false, offset);
        }


        private void Card_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is NutritionModel model)
            {
                // Переход на страницу рецепта
                NavigationService.Navigate(new RecipePage(model));
            }
        }


    }
}
