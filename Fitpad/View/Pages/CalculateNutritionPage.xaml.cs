using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fitpad.ViewModel.PagesViewModels;
using Fitpad.Model.Entities;

namespace Fitpad.View.Pages
{
    public partial class CalculateNutritionPage : Page
    {
        private readonly CalculateNutritionViewModel _viewModel;

        public CalculateNutritionPage()
        {
            InitializeComponent();
            _viewModel = new CalculateNutritionViewModel();
            DataContext = _viewModel;
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Введите название продукта...")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = Brushes.Black;
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var product = await _viewModel.SearchAndAddProductAsync(SearchBox.Text);
                if (product == null)
                {
                    MessageBox.Show("Продукт не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _viewModel.SavedProducts.Add(product);
            }
        }
    }
}
