using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Fitpad.Model.Entities;
using Fitpad.Services;
using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using Fitpad.Model.Repositories;
using Google.Cloud.Firestore;

namespace Fitpad.View.Pages
{
    public partial class ConstructorPage : Page, INotifyPropertyChanged
    {
        private readonly List<NutritionModel> _products = new List<NutritionModel>();
        private readonly TranslatorService _translatorService = new TranslatorService();
        private readonly SeriesCollection _macroSeries;

        private string _pendingProductName;
        private double _pendingCalories, _pendingProtein, _pendingFats, _pendingCarbs, _pendingSugar;
        private bool _isFavorite;
        private bool _isSaveEnabled;

        private static readonly HttpClient _http = new HttpClient(); // общий клиент для USDA

        public ConstructorPage()
        {
            InitializeComponent();
            ProductListBox.ItemsSource = _products;

            UpdateTotalKBJU();

            _macroSeries = new SeriesCollection
            {
                new PieSeries { Title = "Білки",     Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Blue  },
                new PieSeries { Title = "Жири",      Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Red   },
                new PieSeries { Title = "Вуглеводи", Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Green },
                new PieSeries { Title = "Цукор",     Values = new ChartValues<double> { 0 }, DataLabels = true, Fill = Brushes.Orange}
            };
            MacroChart.Series = _macroSeries;
        }

        public bool IsSaveEnabled
        {
            get => _isSaveEnabled;
            set { _isSaveEnabled = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ----- UI helpers
        private void InputFields_TextChanged(object sender, TextChangedEventArgs e) => ValidateInputs();

        private void ProductSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ProductSearchBox.Text))
                PlaceholderText.Visibility = Visibility.Collapsed;
        }

        private void ProductSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProductSearchBox.Text))
                PlaceholderText.Visibility = Visibility.Visible;
        }

        private void ValidateInputs()
        {
            IsSaveEnabled =
                !string.IsNullOrWhiteSpace(DishNameBox.Text) &&
                !string.IsNullOrWhiteSpace(RecipeBox.Text) &&
                !string.IsNullOrWhiteSpace(CookingTimeBox.Text);
        }

        private void UpdateTotalKBJU()
        {
            var totalCalories = _products.Sum(p => p.Calories);
            var totalProtein = _products.Sum(p => p.Protein);
            var totalFats = _products.Sum(p => p.Fats);
            var totalCarbs = _products.Sum(p => p.Carbs);
            var totalSugar = _products.Sum(p => p.Sugar);

            TotalCaloriesText.Text = $"Калорії: {totalCalories:F1} ккал";
            TotalProteinText.Text = $"Білки: {totalProtein:F1} г";
            TotalFatsText.Text = $"Жири: {totalFats:F1} г";
            TotalCarbsText.Text = $"Вуглеводи: {totalCarbs:F1} г";
            TotalSugarText.Text = $"Цукор: {totalSugar:F1} г";
        }

        // ----- Избранное
        private async Task LoadFavoriteStatus()
        {
            var userId = UserSession.CurrentUserId;
            var dishName = DishNameBox.Text;

            var firestoreService = new FirestoreService();
            var favorites = await firestoreService.GetFavoriteDishes(userId);

            _isFavorite = favorites.Any(d => d.Name == dishName);
            FavoriteIcon.Source = new BitmapImage(new Uri(_isFavorite
                ? "pack://siteoforigin:,,,/Images/star_yellow.png"
                : "pack://siteoforigin:,,,/Images/star_grey.png"));
        }

        private void MarkFavorite_Click(object sender, RoutedEventArgs e)
        {
            _isFavorite = !_isFavorite;
            FavoriteIcon.Source = new BitmapImage(new Uri(_isFavorite
                ? "pack://siteoforigin:,,,/Images/star_yellow.png"
                : "pack://siteoforigin:,,,/Images/star_grey.png"));
        }

        // ----- Добавление продукта (USDA)
        private async void SearchProduct_Click(object sender, RoutedEventArgs e)
        {
            var productName = ProductSearchBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(productName))
            {
                MessageBox.Show("Введіть назву продукту!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var translatedName = await _translatorService.TranslateTextAsync(productName, "en");
            if (string.IsNullOrWhiteSpace(translatedName))
            {
                MessageBox.Show("Помилка перекладу назви продукту!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var product = await FetchProductFromUSDA(translatedName);
            if (product == null)
            {
                MessageBox.Show("Продукт не знайдено!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // значения на 100 г
            _pendingCalories = GetNutrientValue(product, "Energy")
                               ?? GetNutrientValue(product, "Energy (Atwater General Factors)")
                               ?? 0;
            _pendingProtein = GetNutrientValue(product, "Protein") ?? 0;
            _pendingFats = FindNutrientContains(product, "fat");
            _pendingCarbs = FindNutrientContains(product, "carbohydrate");
            _pendingSugar = FindNutrientContains(product, "sugar");

            _pendingProductName = product.Description;

            OverlayCanvas.Visibility = Visibility.Visible;
            QuantityInputPanel.Visibility = Visibility.Visible;
        }

        // безопасный поиск нутриента по точному имени
        private static double? GetNutrientValue(USDAFood product, string exactName)
        {
            if (product == null || product.FoodNutrients == null) return null;
            foreach (var n in product.FoodNutrients)
            {
                if (string.Equals(n.NutrientName, exactName, StringComparison.Ordinal))
                    return n.Value ?? 0;
            }
            return null;
        }

        // безопасный поиск по частичному совпадению без Contains(StringComparison)
        private static double FindNutrientContains(USDAFood product, string fragmentLower)
        {
            if (product == null || product.FoodNutrients == null) return 0;
            foreach (var n in product.FoodNutrients)
            {
                var name = n.NutrientName ?? "";
                if (name.IndexOf(fragmentLower, StringComparison.OrdinalIgnoreCase) >= 0)
                    return n.Value ?? 0;
            }
            return 0;
        }

        private void ConfirmQuantity_Click(object sender, RoutedEventArgs e)
        {
            double quantity;
            if (!double.TryParse(QuantityBox.Text, out quantity) || quantity <= 0)
            {
                MessageBox.Show("Будь ласка, введіть коректну кількість.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var comboItem = UnitComboBox.SelectedItem as ComboBoxItem;
            var unit = comboItem != null ? comboItem.Content.ToString() : null;
            if (string.IsNullOrEmpty(unit))
            {
                MessageBox.Show("Будь ласка, виберіть одиницю виміру.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var grams = ConvertToGrams(quantity, unit);
            var factor = grams / 100.0;

            var product = new NutritionModel
            {
                Name = _pendingProductName,
                Quantity = quantity,
                Unit = unit,
                QuantityInGrams = grams,
                Calories = _pendingCalories * factor,
                Protein = _pendingProtein * factor,
                Fats = _pendingFats * factor,
                Carbs = _pendingCarbs * factor,
                Sugar = _pendingSugar * factor
            };

            _products.Add(product);
            ProductListBox.ItemsSource = null;
            ProductListBox.ItemsSource = _products;

            UpdateTotalKBJU();
            UpdatePieChart();

            OverlayCanvas.Visibility = Visibility.Collapsed;
            QuantityInputPanel.Visibility = Visibility.Collapsed;
        }

        private void CancelQuantity_Click(object sender, RoutedEventArgs e)
        {
            OverlayCanvas.Visibility = Visibility.Collapsed;
            QuantityInputPanel.Visibility = Visibility.Collapsed;
        }

        private static double ConvertToGrams(double quantity, string unit)
        {
            switch (unit)
            {
                case "г": return quantity;
                case "кг": return quantity * 1000;
                case "шт": return quantity * 200;  // упрощённое допущение
                case "л": return quantity * 1000; // вода
                default: return quantity;
            }
        }

        // ----- Диаграмма
        private void UpdatePieChart()
        {
            _macroSeries.Clear();

            var totalProtein = _products.Sum(p => p.Protein);
            var totalFats = _products.Sum(p => p.Fats);
            var totalCarbs = _products.Sum(p => p.Carbs);
            var totalSugar = _products.Sum(p => p.Sugar);

            if (totalProtein == 0 && totalFats == 0 && totalCarbs == 0 && totalSugar == 0)
            {
                SetDefaultChart();
            }
            else
            {
                _macroSeries.Add(new PieSeries { Title = "Білки", Values = new ChartValues<double> { totalProtein }, DataLabels = true, Fill = Brushes.Blue });
                _macroSeries.Add(new PieSeries { Title = "Жири", Values = new ChartValues<double> { totalFats }, DataLabels = true, Fill = Brushes.Red });
                _macroSeries.Add(new PieSeries { Title = "Вуглеводи", Values = new ChartValues<double> { totalCarbs }, DataLabels = true, Fill = Brushes.Green });
                _macroSeries.Add(new PieSeries { Title = "Цукор", Values = new ChartValues<double> { totalSugar }, DataLabels = true, Fill = Brushes.Orange });
            }

            MacroChart.Series = null;
            MacroChart.Series = _macroSeries;
        }

        private void ProductListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Для простоты диаграмму не перестраиваем под один выбранный продукт
        }

        private void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button != null ? button.DataContext as NutritionModel : null;
            if (product == null) return;

            _products.Remove(product);
            ProductListBox.Items.Refresh();
            UpdateTotalKBJU();
            UpdatePieChart();
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button != null ? button.DataContext as NutritionModel : null;
            if (product == null) return;

            _pendingProductName = product.Name;
            _pendingCalories = product.Calories;
            _pendingProtein = product.Protein;
            _pendingFats = product.Fats;
            _pendingCarbs = product.Carbs;
            _pendingSugar = product.Sugar;

            QuantityBox.Text = product.Quantity.ToString();
            OverlayCanvas.Visibility = Visibility.Visible;
            QuantityInputPanel.Visibility = Visibility.Visible;

            _products.Remove(product);
            ProductListBox.Items.Refresh();
        }

        private void UnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QuantityBox == null || UnitComboBox.SelectedItem == null) return;

            double q;
            if (!double.TryParse(QuantityBox.Text, out q)) { QuantityBox.Text = "0"; return; }

            var comboItem = UnitComboBox.SelectedItem as ComboBoxItem;
            var unit = comboItem != null ? comboItem.Content.ToString() : "";

            var grams = ConvertToGrams(q, unit);
            QuantityBox.Text = grams.ToString("0.##");
        }

        private void SetDefaultChart()
        {
            _macroSeries.Clear();
            _macroSeries.Add(new PieSeries
            {
                Values = new ChartValues<double> { 1 },
                DataLabels = false,
                Fill = Brushes.Orange,
                IsHitTestVisible = false,
                Title = ""
            });
            MacroChart.Series = _macroSeries;
        }

        // ----- Сохранение блюда
        private string GetCurrentUserId() => UserSession.CurrentUserId;

        private async void SaveDish_Click(object sender, RoutedEventArgs e)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(DishNameBox.Text) ||
                string.IsNullOrWhiteSpace(RecipeBox.Text) ||
                string.IsNullOrWhiteSpace(CookingTimeBox.Text))
            {
                MessageBox.Show("Будь ласка, заповніть всі обов'язкові поля!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var totalWeight = _products.Sum(p => p.QuantityInGrams);
            var totalCal = _products.Sum(p => p.Calories);
            var totalProt = _products.Sum(p => p.Protein);
            var totalFat = _products.Sum(p => p.Fats);
            var totalCarb = _products.Sum(p => p.Carbs);

            var factor100g = totalWeight > 0 ? 100.0 / totalWeight : 0.0;

            var fs = new FirestoreService();
            var dishId = fs.GenerateDishId();
            var now = Timestamp.FromDateTime(DateTime.UtcNow);

            var dish = new DishModel
            {
                Id = dishId,
                UserId = userId,
                Name = DishNameBox.Text,
                CookingTime = CookingTimeBox.Text,
                Recipe = RecipeBox.Text,
                Ingredients = _products.Select(p => p.Name).ToList(),
                IsFavorite = _isFavorite,

                CaloriesPerUnit = Math.Round(totalCal * factor100g, 2),
                ProteinPerUnit = Math.Round(totalProt * factor100g, 2),
                FatPerUnit = Math.Round(totalFat * factor100g, 2),
                CarbPerUnit = Math.Round(totalCarb * factor100g, 2),

                DefaultServingGrams = 100,
                CreatedAt = now,
                UpdatedAt = now
            };

            Console.WriteLine($"📌 Дані перед збереженням: {JsonConvert.SerializeObject(dish, Formatting.Indented)}");
            await fs.SaveDishToFirebase(userId, dish);

            MessageBox.Show("Блюдо успішно збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

            ResetForm();
            NavigateToDishesPage();
        }

        private void ResetForm()
        {
            DishNameBox.Text = string.Empty;
            CookingTimeBox.Text = string.Empty;
            RecipeBox.Text = string.Empty;
            ProductSearchBox.Text = string.Empty;

            _products.Clear();
            ProductListBox.ItemsSource = null;
            ProductListBox.ItemsSource = _products;

            _isFavorite = false;
            FavoriteIcon.Source = new BitmapImage(new Uri("pack://siteoforigin:,,,/Images/star_grey.png", UriKind.RelativeOrAbsolute));

            TotalCaloriesText.Text = "Калорії: 0.0 ккал";
            TotalProteinText.Text = "Білки: 0.0 г";
            TotalFatsText.Text = "Жири: 0.0 г";
            TotalCarbsText.Text = "Вуглеводи: 0.0 г";
            TotalSugarText.Text = "Цукор: 0.0 г";

            SetDefaultChart();
        }

        private void NavigateToDishesPage()
        {
            if (NavigationService == null) return;

            var existing = NavigationService.Content as DishesPage;
            if (existing != null)
            {
                existing.RefreshDishesList();
            }
            else
            {
                NavigationService.Navigate(new DishesPage());
            }
        }

        // ----- USDA API
        private async Task<USDAFood> FetchProductFromUSDA(string productName)
        {
            const string apiKey = "vTsUonfbJdkCXVp8JiZ8nt1FC36J2ldKeqGAuDUJ";
            var url = $"https://api.nal.usda.gov/fdc/v1/foods/search?query={WebUtility.UrlEncode(productName)}&api_key={apiKey}";

            try
            {
                var resp = await _http.GetAsync(url);
                var json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) return null;

                var result = System.Text.Json.JsonSerializer.Deserialize<USDAResponse>(json);
                return result != null && result.Foods != null ? result.Foods.FirstOrDefault() : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"USDA error: {ex.Message}");
                return null;
            }
        }

        // ---- DTOs для USDA
        private class USDAResponse
        {
            [JsonPropertyName("foods")]
            public List<USDAFood> Foods { get; set; }
        }

        private class USDAFood
        {
            [JsonPropertyName("fdcId")] public int FdcId { get; set; }
            [JsonPropertyName("description")] public string Description { get; set; }
            [JsonPropertyName("foodNutrients")] public List<USDANutrient> FoodNutrients { get; set; }
        }

        private class USDANutrient
        {
            [JsonPropertyName("nutrientName")] public string NutrientName { get; set; }
            [JsonPropertyName("value")] public double? Value { get; set; }
        }
    }
} 

