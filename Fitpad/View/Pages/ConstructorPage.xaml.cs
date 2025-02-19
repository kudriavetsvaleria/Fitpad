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
using static Fitpad.View.Pages.ConstructorPage;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Fitpad.View.Pages
{
    public partial class ConstructorPage : Page, INotifyPropertyChanged
    {
        private List<NutritionModel> _products = new List<NutritionModel>();
        private TranslatorService _translatorService = new TranslatorService();
        private SeriesCollection _macroSeries;
        private string _pendingProductName;
        private bool isFavorite = false;

        private double _pendingCalories;
        private double _pendingProtein;
        private double _pendingFats;
        private double _pendingCarbs;
        private double _pendingSugar;

        private bool _isSaveEnabled;

        public bool IsSaveEnabled
        {
            get => _isSaveEnabled;
            set
            {
                _isSaveEnabled = value;
                OnPropertyChanged();
            }
        }
        private void UpdateTotalKBJU()
        {
            double totalCalories = _products.Sum(p => p.Calories);
            double totalProtein = _products.Sum(p => p.Protein);
            double totalFats = _products.Sum(p => p.Fats);
            double totalCarbs = _products.Sum(p => p.Carbs);
            double totalSugar = _products.Sum(p => p.Sugar);

            TotalCaloriesText.Text = $"Калорії: {totalCalories:F1} ккал";
            TotalProteinText.Text = $"Білки: {totalProtein:F1} г";
            TotalFatsText.Text = $"Жири: {totalFats:F1} г";
            TotalCarbsText.Text = $"Вуглеводи: {totalCarbs:F1} г";
            TotalSugarText.Text = $"Цукор: {totalSugar:F1} г";
        }


        public ConstructorPage()
        {
            InitializeComponent();
            ProductListBox.ItemsSource = _products;

            // ✅ Обновляем КБЖУ при загрузке
            UpdateTotalKBJU();

            _macroSeries = new SeriesCollection
            {
                new PieSeries { Title = "Білки", Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Blue },
                new PieSeries { Title = "Жири", Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Red },
                new PieSeries { Title = "Вуглеводи", Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Green },
                new PieSeries { Title = "Цукор", Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Orange }
            };

            MacroChart.Series = _macroSeries;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private void InputFields_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInputs();
        }

        private void ProductSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ProductSearchBox.Text == "")
            {
                PlaceholderText.Visibility = Visibility.Collapsed; // Скрываем placeholder
            }
        }

        private void ProductSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProductSearchBox.Text))
            {
                PlaceholderText.Visibility = Visibility.Visible; // Показываем placeholder снова
            }
        }


        private void ValidateInputs()
        {
            IsSaveEnabled = !string.IsNullOrWhiteSpace(DishNameBox.Text)
                            && !string.IsNullOrWhiteSpace(RecipeBox.Text)
                            && !string.IsNullOrWhiteSpace(CookingTimeBox.Text);
            OnPropertyChanged(nameof(IsSaveEnabled)); // 🔹 Обновляем привязку кнопки!
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

        private void MarkFavorite_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("⭐ Клик по кнопке избранного!");

            if (sender is Button button)
            {
                // Инвертируем текущее состояние избранного
                isFavorite = !isFavorite;

                Console.WriteLine($"✅ Новый статус избранного: {isFavorite}");

                // Обновляем иконку звезды
                string newImage = isFavorite
                    ? "pack://siteoforigin:,,,/Images/star_yellow.png"
                    : "pack://siteoforigin:,,,/Images/star_grey.png";

                Dispatcher.Invoke(() =>
                {
                    if (FavoriteIcon != null)
                    {
                        Console.WriteLine("🔄 Обновление иконки избранного...");
                        FavoriteIcon.Source = new BitmapImage(new Uri(newImage, UriKind.RelativeOrAbsolute));
                        Console.WriteLine("✅ Иконка обновлена!");
                    }
                });
            }
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

            // 🔹 Пересчитываем в граммы для расчётов
            double quantityInGrams = ConvertToGrams(quantity, unit);

            // 🔹 Пересчитываем КБЖУ с учетом введенного количества
            double factor = quantityInGrams / 100.0;
            double calories = _pendingCalories * factor;
            double protein = _pendingProtein * factor;
            double fats = _pendingFats * factor;
            double carbs = _pendingCarbs * factor;
            double sugar = _pendingSugar * factor;

            // ✅ Добавляем обновленный продукт в список
            NutritionModel updatedProduct = new NutritionModel
            {
                Name = _pendingProductName,
                Quantity = quantity,
                Unit = unit, // Отображаемая единица измерения
                QuantityInGrams = quantityInGrams, // Для расчётов
                Calories = calories,
                Protein = protein,
                Fats = fats,
                Carbs = carbs,
                Sugar = sugar
            };

            _products.Add(updatedProduct);

            // ✅ Обновляем ListBox
            ProductListBox.ItemsSource = null;
            ProductListBox.ItemsSource = _products;

            // ✅ Обновляем КБЖУ
            UpdateTotalKBJU();

            // ✅ Обновляем диаграмму КБЖУ
            UpdatePieChart();

            // ✅ Закрываем окно
            OverlayCanvas.Visibility = Visibility.Collapsed;
            QuantityInputPanel.Visibility = Visibility.Collapsed;
        }

        private double ConvertToGrams(double quantity, string unit)
        {
            switch (unit)
            {
                case "г":
                    return quantity; // Уже в граммах
                case "кг":
                    return quantity * 1000; // 1 кг = 1000 г
                case "шт":
                    return quantity * 200; // Средний вес 1 штуки (пример, можно сделать API-запрос)
                case "л":
                    return quantity * 1000; // 1 литр = 1000 г (упрощение для воды)
                default:
                    return quantity; // По умолчанию, если неизвестная единица
            }
        }


        private void UpdatePieChart()
        {
            _macroSeries.Clear();

            double totalProtein = _products.Sum(p => p.Protein);
            double totalFats = _products.Sum(p => p.Fats);
            double totalCarbs = _products.Sum(p => p.Carbs);
            double totalSugar = _products.Sum(p => p.Sugar);

            if (totalProtein == 0 && totalFats == 0 && totalCarbs == 0 && totalSugar == 0)
            {
                SetDefaultChart();
                return;
            }

            _macroSeries.Add(new PieSeries { Title = "Білки", Values = new ChartValues<double> { totalProtein }, DataLabels = true, Fill = Brushes.Blue });
            _macroSeries.Add(new PieSeries { Title = "Жири", Values = new ChartValues<double> { totalFats }, DataLabels = true, Fill = Brushes.Red });
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


        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Text == "Введіть назву продукту...")
            {
                textBox.Text = string.Empty;
                textBox.Foreground = Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Введіть назву продукту...";
                textBox.Foreground = Brushes.Gray;
            }
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
                _pendingFats = productData.FoodNutrients?.FirstOrDefault(n => n.NutrientName.Contains("lipid"))?.Value ?? 0;
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
            string userId = GetCurrentUserId();

            // Проверяем, заполнены ли поля перед сохранением
            if (string.IsNullOrWhiteSpace(DishNameBox.Text) ||
                string.IsNullOrWhiteSpace(RecipeBox.Text) ||
                string.IsNullOrWhiteSpace(CookingTimeBox.Text))
            {
                MessageBox.Show("Будь ласка, заповніть всі обов'язкові поля!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            FirestoreService firestoreService = new FirestoreService();
            string dishId = firestoreService.GenerateDishId();

            var dish = new DishModel
            {
                Id = dishId,
                UserId = userId,
                Name = DishNameBox.Text,
                CookingTime = CookingTimeBox.Text,
                Recipe = RecipeBox.Text,
                Ingredients = _products.Select(p => p.Name).ToList(),
                IsFavorite = isFavorite
            };

            Console.WriteLine($"📌 Дані перед збереженням: {JsonConvert.SerializeObject(dish, Formatting.Indented)}");

            await firestoreService.SaveDishToFirebase(dish);

            MessageBox.Show("Блюдо успішно збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

            // ✅ Сбрасываем все поля после успешного сохранения
            ResetForm();

            // ✅ Переход на страницу DishesPage
            NavigateToDishesPage();
        }


        /// <summary>
        /// Метод для перехода на страницу DishesPage и обновления списка блюд
        /// </summary>
        private void NavigateToDishesPage()
        {
            if (NavigationService != null)
            {
                var existingPage = NavigationService.Content as DishesPage; // Проверяем, загружена ли уже DishesPage

                if (existingPage != null)
                {
                    Console.WriteLine("✅ Используем существующую страницу DishesPage.");
                    existingPage.RefreshDishesList(); // Обновляем список блюд
                }
                else
                {
                    Console.WriteLine("📌 Создаем новый экземпляр DishesPage.");
                    DishesPage newDishesPage = new DishesPage();
                    NavigationService.Navigate(newDishesPage);
                }
            }
            else
            {
                Console.WriteLine("❌ Ошибка: NavigationService = null");
            }
        }


        private void ResetForm()
        {
            Console.WriteLine("🔄 Сброс формы после сохранения блюда...");

            // Очистка текстовых полей
            DishNameBox.Text = string.Empty;
            CookingTimeBox.Text = string.Empty;
            RecipeBox.Text = string.Empty;
            ProductSearchBox.Text = string.Empty; // ✅ Очистка поля "Додати продукти"

            // Очистка списка продуктов
            _products.Clear();
            ProductListBox.ItemsSource = null;
            ProductListBox.ItemsSource = _products;

            // Сброс состояния избранного (делаем серую иконку)
            isFavorite = false;
            string newImage = "pack://siteoforigin:,,,/Images/star_grey.png";
            FavoriteIcon.Source = new BitmapImage(new Uri(newImage, UriKind.RelativeOrAbsolute));

            // ✅ Обнуление информации КБЖУ
            TotalCaloriesText.Text = "Калорії: 0.0 ккал";
            TotalProteinText.Text = "Білки: 0.0 г";
            TotalFatsText.Text = "Жири: 0.0 г";
            TotalCarbsText.Text = "Вуглеводи: 0.0 г";
            TotalSugarText.Text = "Цукор: 0.0 г";

            // ✅ Сброс диаграммы КБЖУ
            SetDefaultChart();

            Console.WriteLine("✅ Форма сброшена!");
        }


        private void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is NutritionModel product)
            {
                _products.Remove(product);
                ProductListBox.Items.Refresh();

                // ✅ Обновляем КБЖУ после удаления
                UpdateTotalKBJU();

                // ✅ Обновляем диаграмму после удаления
                UpdatePieChart();

                Console.WriteLine($"❌ Продукт {product.Name} удалён.");
            }
        }


        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is NutritionModel product)
            {
                Console.WriteLine($"✏️ Изменение продукта: {product.Name}");

                // Запоминаем редактируемый продукт
                _pendingProductName = product.Name;
                _pendingCalories = product.Calories;
                _pendingProtein = product.Protein;
                _pendingFats = product.Fats;
                _pendingCarbs = product.Carbs;
                _pendingSugar = product.Sugar;

                // Заполняем поля ввода
                QuantityBox.Text = product.Quantity.ToString();

                // Отображаем окно для редактирования
                OverlayCanvas.Visibility = Visibility.Visible;
                QuantityInputPanel.Visibility = Visibility.Visible;

                // Удаляем продукт из списка перед обновлением (чтобы не дублировался)
                _products.Remove(product);
                ProductListBox.Items.Refresh();
            }
        }

        private void UnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QuantityBox == null || UnitComboBox.SelectedItem == null)
                return;

            string selectedUnit = (UnitComboBox.SelectedItem as ComboBoxItem).Content.ToString();
            double quantity;

            if (!double.TryParse(QuantityBox.Text, out quantity))
            {
                QuantityBox.Text = "0"; // Если пользователь ввел неверные данные
                return;
            }

            double quantityInGrams = ConvertToGrams(quantity, selectedUnit);

            // Сохранение данных
            QuantityBox.Text = quantityInGrams.ToString("0.##"); // Округляем до 2 знаков
        }


        // 🔹 Вызови `UpdateTotalKBJU()` после сохранения изменений
        private void SaveEditedProduct()
        {
            // ✅ После сохранения обновляем КБЖУ
            UpdateTotalKBJU();

            // ✅ Обновляем диаграмму после редактирования
            UpdatePieChart();
        }



        private void ProductListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductListBox.SelectedItem is NutritionModel selectedProduct)
            {
                Console.WriteLine($"🔹 Выбран продукт: {selectedProduct.Name}");
                Console.WriteLine($"Б: {selectedProduct.Protein}, Ж: {selectedProduct.Fats}, В: {selectedProduct.Carbs}, С: {selectedProduct.Sugar}");

                UpdatePieChart(selectedProduct.Protein, selectedProduct.Fats, selectedProduct.Carbs, selectedProduct.Sugar);
            }
        }


        private void UpdatePieChart(double protein, double Fats, double carbs, double sugar)
        {
            _macroSeries.Clear();

            double totalProtein = _products.Sum(p => p.Protein);
            double totalFats = _products.Sum(p => p.Fats);
            double totalCarbs = _products.Sum(p => p.Carbs);
            double totalSugar = _products.Sum(p => p.Sugar);

            if (totalProtein == 0 && totalFats == 0 && totalCarbs == 0 && totalSugar == 0)
            {
                SetDefaultChart();
                return;
            }

            _macroSeries.Add(new PieSeries { Title = "Білки", Values = new ChartValues<double> { protein }, DataLabels = true, Fill = Brushes.Blue });
            _macroSeries.Add(new PieSeries { Title = "Жири", Values = new ChartValues<double> { Fats }, DataLabels = true, Fill = Brushes.Red });
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
                Values = new ChartValues<double> { 1 }, // Значение для отображения
                DataLabels = false, // ❌ Отключаем подписи
                Fill = Brushes.Orange, // Оранжевый цвет
                IsHitTestVisible = false, // ❌ Отключаем всплывающие подсказки
                Title = "" // ❌ Очищаем текст "Series 1"
            });

            MacroChart.Series = _macroSeries;
        }


        public class Product
        {
            public string ProductName { get; set; }
        }

    }
}