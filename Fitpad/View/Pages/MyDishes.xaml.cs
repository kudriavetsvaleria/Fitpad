using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fitpad.Model.Entities;
using Fitpad.Services;
using LiveCharts;
using LiveCharts.Wpf;

namespace Fitpad.View.Pages
{
    public partial class MyDishesPage : Page
    {
        private List<ProductItem> _products = new List<ProductItem>();
        private TranslatorService _translatorService = new TranslatorService();
        private SeriesCollection _macroSeries;

        public MyDishesPage()
        {
            InitializeComponent();
            ProductListBox.ItemsSource = _products;
            _macroSeries = new SeriesCollection
            {
                new PieSeries { Title = "Білки", Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Blue },
                new PieSeries { Title = "Жири", Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Red },
                new PieSeries { Title = "Вуглеводи", Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Green },
                new PieSeries { Title = "Сахар", Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Orange }
            };

            MacroChart.Series = _macroSeries;
            SetDefaultChart();
        }

        private async void SearchProduct_Click(object sender, RoutedEventArgs e)
        {
            string productName = ProductSearchBox.Text;
            if (string.IsNullOrWhiteSpace(productName))
            {
                MessageBox.Show("Введіть назву продукту!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Console.WriteLine($"🔹 Введено: {productName}");

            // Переводим название продукта на английский (если необходимо)
            string translatedName = await _translatorService.TranslateTextAsync(productName, "en");
            Console.WriteLine($"🔹 Переведено: {translatedName}");

            USDAFood productData = await FetchProductFromUSDA(translatedName);

            if (productData != null)
            {
                double calories = productData.FoodNutrients?.Find(n => n.NutrientName == "Energy")?.Value ?? 0;
                double protein = productData.FoodNutrients?.Find(n => n.NutrientName == "Protein")?.Value ?? 0;
                double fat = productData.FoodNutrients?.Find(n => n.NutrientName == "Total lipid (fat)")?.Value ?? 0;
                double carbs = productData.FoodNutrients?.Find(n => n.NutrientName.Contains("Carbohydrate"))?.Value ?? 0;
                double sugar = productData.FoodNutrients?.Find(n => n.NutrientName.Contains("Sugars"))?.Value ?? 0;

                ProductItem newProduct = new ProductItem
                {
                    Index = _products.Count + 1, // Добавляем индекс
                    Name = productData.Description,
                    Calories = calories,
                    Protein = protein,
                    Fat = fat,
                    Carbs = carbs,
                    Sugar = sugar
                };

                _products.Add(newProduct);
                ProductListBox.ItemsSource = null;
                ProductListBox.ItemsSource = _products;
            }
            else
            {
                MessageBox.Show("Продукт не знайдено!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ProductListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductListBox.SelectedItem is ProductItem selectedProduct)
            {
                // Обновляем диаграмму
                UpdatePieChart(selectedProduct.Protein, selectedProduct.Fat, selectedProduct.Carbs, selectedProduct.Sugar);
            }
        }

        private void UpdatePieChart(double protein, double fat, double carbs, double sugar)
        {
            _macroSeries.Clear();

            if (protein == 0 && fat == 0 && carbs == 0 && sugar == 0)
            {
                SetDefaultChart(); // Если данных нет, показываем серый круг
                return;
            }

            // Сбрасываем отступ, если диаграмма не пустая

            _macroSeries.Add(new PieSeries { Title = "Білки", Values = new ChartValues<double> { protein }, DataLabels = true, Fill = Brushes.Blue });
            _macroSeries.Add(new PieSeries { Title = "Жири", Values = new ChartValues<double> { fat }, DataLabels = true, Fill = Brushes.Red });
            _macroSeries.Add(new PieSeries { Title = "Вуглеводи", Values = new ChartValues<double> { carbs }, DataLabels = true, Fill = Brushes.Green });
            _macroSeries.Add(new PieSeries { Title = "Сахар", Values = new ChartValues<double> { sugar }, DataLabels = true, Fill = Brushes.Orange });
        }



        private async Task<USDAFood> FetchProductFromUSDA(string productName)
        {
            using HttpClient client = new HttpClient();
            string apiKey = "vTsUonfbJdkCXVp8JiZ8nt1FC36J2ldKeqGAuDUJ";
            string searchUrl = $"https://api.nal.usda.gov/fdc/v1/foods/search?query={WebUtility.UrlEncode(productName)}&api_key={apiKey}";

            Console.WriteLine($"🔹 Запрос к USDA API: {searchUrl}");

            try
            {
                HttpResponseMessage response = await client.GetAsync(searchUrl);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // 🔹 Логируем ВЕСЬ JSON
                Console.WriteLine($"🔹 Полный JSON-ответ: {jsonResponse}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Ошибка HTTP: {response.StatusCode}");
                    return null;
                }

                // ✅ Исправленный разбор JSON
                var result = JsonSerializer.Deserialize<USDAResponse>(jsonResponse);
                if (result != null && result.Foods != null && result.Foods.Count > 0)
                {
                    USDAFood foundFood = result.Foods[0]; // Берём ПЕРВЫЙ продукт из списка
                    Console.WriteLine($"✅ Найден продукт: {foundFood.Description}");
                    return foundFood;
                }
                else
                {
                    Console.WriteLine("❌ Продукт не найден в USDA.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при получении данных: {ex.Message}");
            }

            return null;
        }


        private void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProductItem product)
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

            List<string> ingredientNames = new List<string>();
            foreach (var product in _products)
            {
                ingredientNames.Add($"{product.Name} (Калорії: {product.Calories}, Білки: {product.Protein}, Жири: {product.Fat}, Вуглеводи: {product.Carbs})");
            }

            var dish = new Dish
            {
                Name = dishName,
                CookingTime = cookingTime,
                Recipe = recipe,
                Ingredients = ingredientNames
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

        public class USDAResponse
        {
            [JsonPropertyName("foods")] // ✅ Указываем название, которое API возвращает
            public List<USDAFood> Foods { get; set; }
        }

        public class USDAFood
        {
            [JsonPropertyName("fdcId")]
            public int FdcId { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("foodNutrients")]
            public List<USDANutrient> FoodNutrients { get; set; }
        }

        public class USDANutrient
        {
            [JsonPropertyName("nutrientName")]
            public string NutrientName { get; set; }

            [JsonPropertyName("value")]
            public double? Value { get; set; }
        }

        private void SetDefaultChart()
        {
            _macroSeries.Clear();
            _macroSeries.Add(new PieSeries
            {
                Title = "Пусто",
                Values = new ChartValues<double> { 1 },
                DataLabels = false,
                Fill = Brushes.Gray
            });

            MacroChart.Margin = new Thickness(0, 20, 0, 0); // Опускаем диаграмму ниже
        }


        private void AddPhoto_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Додавання фото ще не реалізовано", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MarkFavorite_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Позначення як улюблене ще не реалізовано", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public class Product
        {
            public string ProductName { get; set; }
        }

        private void MacroChart_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}