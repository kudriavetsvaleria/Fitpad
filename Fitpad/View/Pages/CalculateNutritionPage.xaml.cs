using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fitpad.ViewModel.PagesViewModels;
using Fitpad.Model.Entities;
using Fitpad.Services;

namespace Fitpad.View.Pages
{
    public partial class CalculateNutritionPage : Page
    {
        private readonly CalculateNutritionViewModel _viewModel;
        private readonly TranslatorService _translatorService;

        public CalculateNutritionPage()
        {
            InitializeComponent();
            _viewModel = new CalculateNutritionViewModel();
            _translatorService = new TranslatorService();
            DataContext = _viewModel;
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Введите название продукта..." || SearchBox.Text == "Назва продукту...")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Назва продукту...";
                SearchBox.Foreground = Brushes.Gray;
            }
        }



        private void WeightBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(WeightBox.Text))
            {
                WeightBox.Text = "Вага... (г)";
                WeightBox.Foreground = Brushes.Gray;
            }
        }

        private void WeightBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (WeightBox.Text == "Вага... (г)")
            {
                WeightBox.Text = "";
                WeightBox.Foreground = Brushes.Black;
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string productName = SearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(productName) || productName == "Назва продукту...")
            {
                MessageBox.Show("Введіть назву продукту!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(WeightBox.Text.Trim(), out double weight) || weight <= 0)
            {
                MessageBox.Show("Будь ласка, введіть коректну вагу!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Console.WriteLine($"🔹 Введено користувачем: {productName}, вага: {weight} г");

            // Отправляем запрос в API и добавляем продукт с учетом веса
            var product = await _viewModel.SearchAndAddProductAsync(productName, weight);

            if (product != null)
            {
                Console.WriteLine($"✅ Додано продукт: {product.Title}, калорії: {product.Calories}");
            }
            else
            {
                MessageBox.Show("Продукт не знайдено", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

    }
}
