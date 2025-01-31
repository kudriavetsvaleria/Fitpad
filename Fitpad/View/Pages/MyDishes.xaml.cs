using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Fitpad.Services;

namespace Fitpad.View.Pages
{
    public partial class MyDishesPage : Page
    {
        private List<string> _products = new List<string>();
        private TranslatorService _translatorService = new TranslatorService();

        public MyDishesPage()
        {
            InitializeComponent();
            ProductListBox.ItemsSource = _products;
        }

        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderTextBlock.Visibility = string.IsNullOrWhiteSpace(ProductSearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void SearchProduct_Click(object sender, RoutedEventArgs e)
        {
            string productName = ProductSearchBox.Text;
            if (string.IsNullOrWhiteSpace(productName))
            {
                MessageBox.Show("Введіть назву продукту!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 🔹 Логируем введённое значение
            Console.WriteLine($"Введено: {productName}");

            // 🔹 Переводим введённое название на английский перед поиском
            string translatedName = await _translatorService.TranslateTextAsync(productName, "en");

            // 🔹 Логируем перевод
            Console.WriteLine($"Переведено: {translatedName}");

            string productData = await FetchProductFromOpenFoodFacts(translatedName);

            if (!string.IsNullOrEmpty(productData))
            {
                _products.Add(productData);
                ProductListBox.Items.Refresh();
            }
            else
            {
                MessageBox.Show("Продукт не знайдено!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task<string> FetchProductFromOpenFoodFacts(string productName)
        {
            using HttpClient client = new HttpClient();
            string searchUrl = $"https://world.openfoodfacts.org/cgi/search.pl?search_terms={WebUtility.UrlEncode(productName)}&search_simple=1&json=1";

            // 🔹 Логируем URL запроса
            Console.WriteLine($" Запрос к Open Food Facts: {searchUrl}");

            try
            {
                HttpResponseMessage response = await client.GetAsync(searchUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($" Ошибка HTTP: {response.StatusCode}");
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                // 🔹 Логируем JSON-ответ
                Console.WriteLine($" Ответ API: {jsonResponse}");

                var productResult = JsonSerializer.Deserialize<OpenFoodFactsResponse>(jsonResponse);

                if (productResult?.Products?.Count > 0)
                {
                    string foundProduct = productResult.Products[0].ProductName;
                    Console.WriteLine($" Найден продукт: {foundProduct}");
                    return foundProduct;
                }
                else
                {
                    Console.WriteLine(" Продукт не найден в базе Open Food Facts.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Ошибка при получении данных: {ex.Message}");
            }

            return null;
        }

        private void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is string product)
            {
                _products.Remove(product);
                ProductListBox.Items.Refresh();
            }
        }

        private void SaveDish_Click(object sender, RoutedEventArgs e)
        {
            string dishName = DishNameBox.Text;
            string cookingTime = CookingTimeBox.Text;
            string recipe = RecipeBox.Text;

            if (string.IsNullOrWhiteSpace(dishName) || string.IsNullOrWhiteSpace(recipe) || string.IsNullOrWhiteSpace(cookingTime))
            {
                MessageBox.Show("Будь ласка, заповніть усі поля.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dish = new Dish
            {
                Name = dishName,
                CookingTime = cookingTime,
                Recipe = recipe,
                Ingredients = new List<string>(_products)
            };

            SaveDishToFile(dish);
            MessageBox.Show("Рецепт збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveDishToFile(Dish dish)
        {
            string filePath = "MyDishes.json";
            List<Dish> dishes = new List<Dish>();

            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                dishes = JsonSerializer.Deserialize<List<Dish>>(json) ?? new List<Dish>();
            }

            dishes.Add(dish);
            string updatedJson = JsonSerializer.Serialize(dishes, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, updatedJson);
        }
    }

    public class Dish
    {
        public string Name { get; set; }
        public string CookingTime { get; set; }
        public string Recipe { get; set; }
        public List<string> Ingredients { get; set; }
    }

    public class OpenFoodFactsResponse
    {
        public List<Product> Products { get; set; }
    }

    public class Product
    {
        public string ProductName { get; set; }
    }
}
