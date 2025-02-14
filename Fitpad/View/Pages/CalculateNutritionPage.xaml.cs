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
            if (string.IsNullOrWhiteSpace(productName) || productName == "Введите название продукта...")
            {
                MessageBox.Show("Введите название продукта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Console.WriteLine($"🔹 Введено пользователем: {productName}");

            // Переводим название, если требуется API
            string translatedName = await _translatorService.TranslateTextAsync(productName, "en");
            if (string.IsNullOrWhiteSpace(translatedName))
            {
                MessageBox.Show("Ошибка перевода названия продукта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Console.WriteLine($"🔹 Переведено: {translatedName}");

            // Отправляем запрос в API
            var product = await _viewModel.SearchAndAddProductAsync(translatedName);

            if (product != null)
            {
                Console.WriteLine($"✅ Найден продукт: {product.Title}");

                // Проверяем, нет ли уже такого продукта в таблице
                if (!_viewModel.SavedProducts.Any(p => p.Title == product.Title))
                {
                    _viewModel.SavedProducts.Add(product);
                }
                else
                {
                    Console.WriteLine("⚠️ Продукт уже добавлен в таблицу, повторное добавление предотвращено.");
                }
            }
            else
            {
                MessageBox.Show("Продукт не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
