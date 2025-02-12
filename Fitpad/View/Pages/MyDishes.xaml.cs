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
using System.Windows.Media.Imaging;
using System.Linq;
using System.Globalization;
using System.Windows.Data;
using static Fitpad.Services.FirestoreService;
using Fitpad.Model.Repositories;
using Newtonsoft.Json;


namespace Fitpad.View.Pages
{
    public partial class MyDishesPage : Page
    {
        private List<ProductItem> _products = new List<ProductItem>();
        private TranslatorService _translatorService = new TranslatorService();
        private SeriesCollection _macroSeries;
        private string _pendingProductName;
        private bool isFavorite = false;

        private double _pendingCalories;
        private double _pendingProtein;
        private double _pendingFat;
        private double _pendingCarbs;
        private double _pendingSugar;

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

        private async Task LoadFavoriteStatus()
        {
            string userId = "123456"; // Заменить на ID текущего пользователя
            string dishName = DishNameBox.Text;

            FirestoreService firestoreService = new FirestoreService();
            var favoriteDishes = await firestoreService.GetFavoriteDishes(userId);

            // Проверяем, есть ли текущее блюдо в списке избранных
            isFavorite = favoriteDishes.Any(d => d.Name == dishName);

            string newImage = isFavorite ? "pack://siteoforigin:,,,/Images/star_yellow.png" : "pack://siteoforigin:,,,/Images/star_grey.png";
            FavoriteIcon.Source = new BitmapImage(new Uri(newImage));
        }


        private async void MarkFavorite_Click(object sender, RoutedEventArgs e)
        {
            isFavorite = !isFavorite;
            string newImage = isFavorite ? "pack://siteoforigin:,,,/Images/star_yellow.png" : "pack://siteoforigin:,,,/Images/star_grey.png";

            FavoriteIcon.Source = new BitmapImage(new Uri(newImage));

            // Получаем ID текущего пользователя
            string userId = "123456"; // 🔹 Тут должен быть реальный ID пользователя

            // Получаем название блюда
            string dishName = DishNameBox.Text;

            if (string.IsNullOrWhiteSpace(dishName))
            {
                MessageBox.Show("Будь ласка, введіть назву страви!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаем объект блюда
            var dish = new DishModel
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Name = dishName,
                CookingTime = CookingTimeBox.Text,
                Recipe = RecipeBox.Text,
                Ingredients = _products.Select(p => $"{p.Name} (Калорії: {p.Calories}, Білки: {p.Protein}, Жири: {p.Fat}, Вуглеводи: {p.Carbs})").ToList(),
                IsFavorite = isFavorite
            };

            // Отправляем в Firebase
            FirestoreService firestoreService = new FirestoreService();
            await firestoreService.SaveDishToFirebase(dish);
        }

        private void ConfirmQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(QuantityBox.Text, out double quantity) || quantity <= 0)
            {
                MessageBox.Show("Будь ласка, введіть коректну кількість.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string unit = (UnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (string.IsNullOrEmpty(unit))
            {
                MessageBox.Show("Будь ласка, виберіть одиницю виміру.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 🔹 Пересчитываем КБЖУ с учетом введенного количества
            double factor = quantity / 100.0; // Все значения даются на 100 г
            double calories = _pendingCalories * factor;
            double protein = _pendingProtein * factor;
            double fat = _pendingFat * factor;
            double carbs = _pendingCarbs * factor;
            double sugar = _pendingSugar * factor;

            // ✅ Добавляем продукт в список
            ProductItem newProduct = new ProductItem
            {
                Name = _pendingProductName,
                Quantity = quantity,
                Unit = unit,
                Calories = calories,
                Protein = protein,
                Fat = fat,
                Carbs = carbs,
                Sugar = sugar
            };

            _products.Add(newProduct);

            // ✅ Обновляем ListBox
            ProductListBox.ItemsSource = null;
            ProductListBox.ItemsSource = _products;

            // ✅ Обновляем диаграмму КБЖУ
            UpdatePieChart();

            // ✅ Закрываем окно
            OverlayCanvas.Visibility = Visibility.Collapsed;
            QuantityInputPanel.Visibility = Visibility.Collapsed;
        }

        private void UpdatePieChart()
        {
            _macroSeries.Clear();

            double totalProtein = _products.Sum(p => p.Protein);
            double totalFat = _products.Sum(p => p.Fat);
            double totalCarbs = _products.Sum(p => p.Carbs);
            double totalSugar = _products.Sum(p => p.Sugar);

            if (totalProtein == 0 && totalFat == 0 && totalCarbs == 0 && totalSugar == 0)
            {
                SetDefaultChart();
                return;
            }

            _macroSeries.Add(new PieSeries { Title = "Білки", Values = new ChartValues<double> { totalProtein }, DataLabels = true, Fill = Brushes.Blue });
            _macroSeries.Add(new PieSeries { Title = "Жири", Values = new ChartValues<double> { totalFat }, DataLabels = true, Fill = Brushes.Red });
            _macroSeries.Add(new PieSeries { Title = "Вуглеводи", Values = new ChartValues<double> { totalCarbs }, DataLabels = true, Fill = Brushes.Green });
            _macroSeries.Add(new PieSeries { Title = "Цукор", Values = new ChartValues<double> { totalSugar }, DataLabels = true, Fill = Brushes.Orange });

            // ✅ Обновляем диаграмму
            MacroChart.Series = null;
            MacroChart.Series = _macroSeries;
        }



        private void CancelQuantity_Click(object sender, RoutedEventArgs e)
        {
            // Закрываем всплывающее окно
            OverlayCanvas.Visibility = Visibility.Collapsed;
            QuantityInputPanel.Visibility = Visibility.Collapsed;
        }

        private async void SearchProduct_Click(object sender, RoutedEventArgs e)
        {
            string productName = ProductSearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(productName))
            {
                MessageBox.Show("Введіть назву продукту!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Console.WriteLine($"🔹 Введено пользователем: {productName}");

            // 🔹 Переводим название на английский (если API требует)
            string translatedName = await _translatorService.TranslateTextAsync(productName, "en");
            if (string.IsNullOrWhiteSpace(translatedName))
            {
                MessageBox.Show("Помилка перекладу назви продукту!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Console.WriteLine($"🔹 Переведено: {translatedName}");

            // 🔹 Отправляем запрос в базу данных USDA
            USDAFood productData = await FetchProductFromUSDA(translatedName);

            if (productData != null)
            {
                Console.WriteLine($"✅ Найден продукт: {productData.Description}");

                // 🔹 Сохраняем КБЖУ для дальнейших расчетов
                _pendingCalories = productData.FoodNutrients?.FirstOrDefault(n => n.NutrientName == "Energy")?.Value ?? 0;
                _pendingProtein = productData.FoodNutrients?.FirstOrDefault(n => n.NutrientName == "Protein")?.Value ?? 0;
                _pendingFat = productData.FoodNutrients?.FirstOrDefault(n => n.NutrientName.Contains("lipid"))?.Value ?? 0;
                _pendingCarbs = productData.FoodNutrients?.FirstOrDefault(n => n.NutrientName.Contains("Carbohydrate"))?.Value ?? 0;
                _pendingSugar = productData.FoodNutrients?.FirstOrDefault(n => n.NutrientName.Contains("Sugars"))?.Value ?? 0;

                _pendingProductName = productData.Description;

                // 🔹 Открываем окно для ввода количества
                OverlayCanvas.Visibility = Visibility.Visible;
                QuantityInputPanel.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Продукт не знайдено!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private string GetCurrentUserId()
        {
            return UserSession.CurrentUserId;
        }

        private async void SaveDish_Click(object sender, RoutedEventArgs e)
        {
            string userId = UserRepository.CurrentUserId;
            Console.WriteLine($"📌 Проверка перед сохранением: UserSession.CurrentUserId = {userId}");

            if (string.IsNullOrEmpty(userId))
            {
                MessageBox.Show("Будь ласка, увійдіть у свій акаунт!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dish = new DishModel
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Name = DishNameBox.Text,
                CookingTime = CookingTimeBox.Text,
                Recipe = RecipeBox.Text,
                Ingredients = _products.Select(p => p.Name).ToList(),
                IsFavorite = isFavorite
            };

            Console.WriteLine($"📌 Данные перед отправкой в Firestore: {JsonConvert.SerializeObject(dish, Formatting.Indented)}");

            FirestoreService firestoreService = new FirestoreService();
            await firestoreService.SaveDishToFirebase(dish);
        }



        private void ProductListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductListBox.SelectedItem is ProductItem selectedProduct)
            {
                Console.WriteLine($"🔹 Выбран продукт: {selectedProduct.Name}");
                Console.WriteLine($"Б: {selectedProduct.Protein}, Ж: {selectedProduct.Fat}, В: {selectedProduct.Carbs}, С: {selectedProduct.Sugar}");

                UpdatePieChart(selectedProduct.Protein, selectedProduct.Fat, selectedProduct.Carbs, selectedProduct.Sugar);
            }
        }


        private void UpdatePieChart(double protein, double fat, double carbs, double sugar)
        {
            _macroSeries.Clear();

            if (protein == 0 && fat == 0 && carbs == 0 && sugar == 0)
            {
                SetDefaultChart();
                return;
            }

            _macroSeries.Add(new PieSeries { Title = "Білки", Values = new ChartValues<double> { protein }, DataLabels = true, Fill = Brushes.Blue });
            _macroSeries.Add(new PieSeries { Title = "Жири", Values = new ChartValues<double> { fat }, DataLabels = true, Fill = Brushes.Red });
            _macroSeries.Add(new PieSeries { Title = "Вуглеводи", Values = new ChartValues<double> { carbs }, DataLabels = true, Fill = Brushes.Green });
            _macroSeries.Add(new PieSeries { Title = "Цукор", Values = new ChartValues<double> { sugar }, DataLabels = true, Fill = Brushes.Orange });

            // ✅ Обновляем диаграмму
            MacroChart.Series = null;
            MacroChart.Series = _macroSeries;
        }


        private async Task<USDAFood> FetchProductFromUSDA(string productName)
        {
            using HttpClient client = new HttpClient();
            string apiKey = "vTsUonfbJdkCXVp8JiZ8nt1FC36J2ldKeqGAuDUJ";
            string searchUrl = $"https://api.nal.usda.gov/fdc/v1/foods/search?query={WebUtility.UrlEncode(productName)}&api_key={apiKey}";

            Console.WriteLine($"🔹 Отправляем запрос к USDA API: {searchUrl}");

            try
            {
                HttpResponseMessage response = await client.GetAsync(searchUrl);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // 🔹 Логируем полный JSON-ответ
                Console.WriteLine($"🔹 Ответ от API: {jsonResponse}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Ошибка запроса: {response.StatusCode}");
                    return null;
                }

                var result = System.Text.Json.JsonSerializer.Deserialize<USDAResponse>(jsonResponse);

                if (result != null && result.Foods != null && result.Foods.Any())
                {
                    USDAFood foundFood = result.Foods.First(); // Берем первый найденный продукт
                    Console.WriteLine($"✅ Найден продукт: {foundFood.Description}");
                    return foundFood;
                }
                else
                {
                    Console.WriteLine("❌ Продукт не найден в базе USDA.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при запросе к API: {ex.Message}");
                return null;
            }
        }

        private void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProductItem product)
            {
                _products.Remove(product);
                ProductListBox.Items.Refresh();
            }
        }

        private void SaveDishToFile(Dish dish)
        {
            string filePath = "MyDishes.json";
            List<Dish> dishes = new List<Dish>();

            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                dishes = System.Text.Json.JsonSerializer.Deserialize<List<Dish>>(json) ?? new List<Dish>();
            }

            dishes.Add(dish);
            string updatedJson = System.Text.Json.JsonSerializer.Serialize(dishes, new JsonSerializerOptions { WriteIndented = true });
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

        private void DisplayImage(string filePath)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            DishImage.Source = bitmap;
            DishImage.Visibility = Visibility.Visible; // Делаем изображение видимым
        }

        private void AddPhoto_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
            };

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                DishImage.Source = new BitmapImage(new Uri(dlg.FileName));
                DishImage.Visibility = Visibility.Visible;

                // Обновляем разметку, чтобы элементы съехали вниз
                DishImage.UpdateLayout();
            }
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