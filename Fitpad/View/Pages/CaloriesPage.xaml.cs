using Fitpad.Model.Entities;
using Fitpad.Services;
using Fitpad.View.Components;
using Google.Cloud.Firestore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Fitpad.View.Pages
{
    public partial class CaloriesPage : Page
    {
        private static CaloriesPage _instance;
        private static UserModel _currentUserCache;
        private readonly FirestoreDb _firestoreDb;


        public CaloriesPage(UserModel currentUser)
        {
            InitializeComponent();
            var firestoreService = new FirestoreService();
            _firestoreDb = firestoreService.GetFirestoreDb();
            _currentUserCache = currentUser;

            Console.WriteLine($"Ініціалізація CaloriesPage для користувача: {currentUser.Name}, ID: {currentUser.Id}");
            InitializePageContentAsync();
        }

        public static CaloriesPage GetInstance(UserModel currentUser)
        {
            if (currentUser == null)
            {
                MessageBox.Show("Помилка: Користувач не знайдений", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            if (_instance == null || _currentUserCache == null || _currentUserCache.Id != currentUser.Id)
            {
                _instance = new CaloriesPage(currentUser);
            }
            return _instance;
        }
        private async void ProductSearchBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (ProductSearchBox.Text.Length >= 2) // Проверяем, что введено минимум 2 символа
            {
                var products = await SearchProductsAsync(ProductSearchBox.Text); // Ищем продукты через API OpenFoodFacts
                ProductSearchBox.ItemsSource = products; // Заполняем выпадающий список найденными продуктами
            }
        }

        private async void InitializePageContentAsync()
        {
            try
            {
                var userInfo = await GetUserInfoAsync(_currentUserCache.Id);

                if (userInfo == null || userInfo.Age == 0 || userInfo.Height == 0 || userInfo.Weight == 0)
                {
                    Console.WriteLine("Дані користувача не знайдено або неповні. Відображення форми введення.");
                    ShowUserInfoForm();
                }
                else
                {
                    Console.WriteLine("Дані користувача завантажено. Відображення добової норми калорій.");
                    ShowCalorieIntake(userInfo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка під час ініціалізації сторінки: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<UserInfoModel> GetUserInfoAsync(string userId)
        {
            try
            {
                var userInfoDoc = await _firestoreDb.Collection("UserInfos").Document(userId).GetSnapshotAsync();

                if (userInfoDoc.Exists)
                {
                    return userInfoDoc.ConvertTo<UserInfoModel>();
                }
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження даних: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private async Task<List<string>> SearchProductsAsync(string query)
        {
            var products = new List<string>();

            try
            {
                using var client = new HttpClient();
                string url = $"https://world.openfoodfacts.org/cgi/search.pl?search_terms={query}&search_simple=1&action=process&json=1";
                var response = await client.GetStringAsync(url);

                var json = JObject.Parse(response);
                var productArray = json["products"];

                if (productArray != null)
                {
                    foreach (var product in productArray)
                    {
                        string productName = product["product_name"]?.ToString();
                        if (!string.IsNullOrEmpty(productName))
                        {
                            products.Add(productName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка під час пошуку продуктів: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return products;
        }

        private async void ProductSearchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductSearchBox.Text.Length >= 2)
            {
                var products = await SearchProductsAsync(ProductSearchBox.Text);
                ProductSearchBox.ItemsSource = products;
            }
        }


        private async Task<dynamic> GetProductDetailsAsync(string productName)
        {
            try
            {
                using var client = new HttpClient();
                string url = $"https://world.openfoodfacts.org/cgi/search.pl?search_terms={productName}&search_simple=1&action=process&json=1";
                var response = await client.GetStringAsync(url);

                var json = JObject.Parse(response);
                var product = json["products"]?.First;

                if (product != null)
                {
                    return new
                    {
                        Name = product["product_name"]?.ToString(),
                        Calories = product["nutriments"]?["energy-kcal_100g"]?.ToObject<double>() ?? 0,
                        Proteins = product["nutriments"]?["proteins_100g"]?.ToObject<double>() ?? 0,
                        Fats = product["nutriments"]?["fat_100g"]?.ToObject<double>() ?? 0,
                        Carbs = product["nutriments"]?["carbohydrates_100g"]?.ToObject<double>() ?? 0
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка під час отримання інформації про продукт: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        private async void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            string productName = ProductSearchBox.Text;
            if (string.IsNullOrWhiteSpace(productName) || !int.TryParse(ProductQuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введіть коректну назву продукту та кількість.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var productDetails = await GetProductDetailsAsync(productName);
            if (productDetails != null)
            {
                double factor = quantity / 100.0;

                ProductsDataGrid.Items.Add(new
                {
                    Name = productDetails.Name,
                    Quantity = quantity,
                    Calories = productDetails.Calories * factor,
                    Proteins = productDetails.Proteins * factor,
                    Fats = productDetails.Fats * factor,
                    Carbs = productDetails.Carbs * factor
                });
            }
        }

        private void ShowUserInfoForm()
        {
            var userInfoForm = new UserInfoForm(_currentUserCache); // Передаем текущего пользователя
            UserInfoFormContainer.Content = userInfoForm; // Добавляем форму в контейнер
            userInfoForm.Visibility = Visibility.Visible;

            DateTextBlock.Visibility = Visibility.Collapsed;
            CalorieTextBlock.Visibility = Visibility.Collapsed;
        }

        private void ShowCalorieIntake(UserInfoModel userInfo)
        {
            double dailyCalories = CalculateDailyCalorieIntake(userInfo);

            // Получаем детали питания
            var (proteins, fats, carbs, fiber, sugar, salt) = CalculateNutritionDetails(userInfo, dailyCalories);

            // Рассчитываем норму воды (предположим, 30 минут активности)
            double dailyWaterIntake = CalculateWaterIntake(userInfo, activityMinutes: 30);

            // Отображаем калорийность и КБЖУ
            CalorieTextBlock.Text = $"0 / {dailyCalories:0} калорій";
            ProteinTextBlock.Text = $"Білки: 0 / {proteins:0} г";
            FatTextBlock.Text = $"Жири: 0 / {fats:0} г";
            CarbTextBlock.Text = $"Вуглеводи: 0 / {carbs:0} г";
            FiberTextBlock.Text = $"Клітковина: 0 / {fiber:0} г";
            SugarTextBlock.Text = $"Цукор: 0 / {sugar:0} г";
            SaltTextBlock.Text = $"Сіль: 0 / {salt:0} г";

            // Отображаем норму воды
            WaterTextBlock.Text = $"Питний режим: {dailyWaterIntake:0.0} л";

            // Делаем все элементы видимыми
            CalorieTextBlock.Visibility = Visibility.Visible;
            ProteinTextBlock.Visibility = Visibility.Visible;
            FatTextBlock.Visibility = Visibility.Visible;
            CarbTextBlock.Visibility = Visibility.Visible;
            FiberTextBlock.Visibility = Visibility.Visible;
            SugarTextBlock.Visibility = Visibility.Visible;
            SaltTextBlock.Visibility = Visibility.Visible;
            WaterTextBlock.Visibility = Visibility.Visible;
        }

        private double CalculateDailyCalorieIntake(UserInfoModel userInfo)
        {
            double bmr;
            double weight = userInfo.Weight;
            double height = userInfo.Height;
            int age = userInfo.Age;
            string gender = userInfo.Gender;
            string activityLevel = userInfo.ActivityLevel;
            string purpose = userInfo.Purpose;

            // Рассчитываем BMR по формуле Миффлина-Сан Жеора
            if (gender == "Чоловік")
            {
                bmr = 88.36 + (13.4 * weight) + (4.8 * height) - (5.7 * age);
            }
            else
            {
                bmr = 447.6 + (9.2 * weight) + (3.1 * height) - (4.3 * age);
            }

            // Множитель активности
            double activityMultiplier = activityLevel switch
            {
                "Низька" => 1.2,
                "Середня" => 1.55,
                "Висока" => 1.9,
                _ => 1.2
            };

            // Рассчитываем TDEE (общее количество калорий)
            double tdee = bmr * activityMultiplier;

            // Корректируем TDEE в зависимости от цели
            tdee = purpose switch
            {
                "Схуднення" => tdee - 400,
                "Набір маси" => tdee + 400,
                _ => tdee
            };

            return tdee;
        }

        private (double Proteins, double Fats, double Carbs, double Fiber, double Sugar, double Salt) CalculateNutritionDetails(UserInfoModel userInfo, double dailyCalories)
        {
            (double ProteinPercent, double FatPercent, double CarbPercent) = userInfo.Purpose switch
            {
                "Похудение" => (0.25, 0.225, 0.475),
                "Набор массы" => (0.175, 0.275, 0.55),
                _ => (0.175, 0.275, 0.55)
            };

            double proteins = (dailyCalories * ProteinPercent) / 4;
            double fats = (dailyCalories * FatPercent) / 9;
            double carbs = (dailyCalories * CarbPercent) / 4;

            double fiber = (userInfo.Gender == "Чоловік" ?
                           (userInfo.Age <= 50 ? 35 : 27) :
                           (userInfo.Age <= 50 ? 23 : 20));

            double sugar = dailyCalories * 0.05 / 4;
            double salt = 5;

            return (proteins, fats, carbs, fiber, sugar, salt);
        }

        private double CalculateWaterIntake(UserInfoModel userInfo, int activityMinutes = 0)
        {
            double waterIntake = userInfo.Weight * 0.035;
            if (activityMinutes > 0)
            {
                waterIntake += (activityMinutes / 30.0) * 0.5;
            }
            return waterIntake;
        }
    }
}
