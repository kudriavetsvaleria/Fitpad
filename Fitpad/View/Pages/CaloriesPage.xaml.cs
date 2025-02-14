using Fitpad.Model.Entities;
using Fitpad.Services;
using Fitpad.View.Components;
using Google.Cloud.Firestore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;


namespace Fitpad.View.Pages
{
    public partial class CaloriesPage : Page
    {
        private static CaloriesPage _instance;
        private static UserModel _currentUserCache;
        private readonly FirestoreDb _firestoreDb;
        private DispatcherTimer _searchTimer;
        private string _lastQuery = string.Empty;
        private DispatcherTimer _debounceTimer;
        private Dictionary<string, List<(string Name, string Id)>> _productCache = new Dictionary<string, List<(string Name, string Id)>>();


        public CaloriesPage(UserModel currentUser)
        {
            InitializeComponent();
            UserInfoFormContainer.Visibility = Visibility.Visible;
            CaloriePageContainer.Visibility = Visibility.Collapsed;
            var firestoreService = new FirestoreService();
            _firestoreDb = firestoreService.GetFirestoreDb();
            _currentUserCache = currentUser;

            // Инициализация таймера для дебаунсинга
            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300) // Задержка 300 мс
            };
            _debounceTimer.Tick += OnDebounceTimerTick;

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

        private async void OnDebounceTimerTick(object sender, EventArgs e)
        {
            _debounceTimer.Stop(); // Останавливаем таймер перед выполнением поиска

            string query = ProductSearchBox.Text.Trim();

            if (string.IsNullOrEmpty(query))
                return;

            if (_productCache.ContainsKey(query))
            {
                ProductSearchBox.ItemsSource = _productCache[query].Select(p => p.Name).ToList();
                ProductSearchBox.IsDropDownOpen = true;
            }
            else
            {
                var products = await SearchProductsAsync(query);
                if (products != null && products.Any())
                {
                    _productCache[query] = products;
                    ProductSearchBox.ItemsSource = products.Select(p => p.Name).ToList();
                    ProductSearchBox.IsDropDownOpen = true;
                }
                else
                {
                    ProductSearchBox.IsDropDownOpen = false;
                }
            }
        }



        private async void ProductSearchBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (ProductSearchBox.Text.Length >= 2) // Проверяем, что введено минимум 2 символа
            {
                var products = await SearchProductsAsync(ProductSearchBox.Text); // Ищем продукты через API OpenFoodFacts
                ProductSearchBox.ItemsSource = products; // Заполняем выпадающий список найденными продуктами
            }
        }

        private void ProductSearchBox_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (ProductSearchBox.Text.Length >= 2)
            {
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        }


        private void ClearContent()
        {
            // Очищаем контейнер формы и скрываем все элементы страницы калорий
            UserInfoFormContainer.Content = null;
            UserInfoFormContainer.Visibility = Visibility.Collapsed;

            CalorieTextBlock.Visibility = Visibility.Collapsed;
            ProteinTextBlock.Visibility = Visibility.Collapsed;
            FatTextBlock.Visibility = Visibility.Collapsed;
            CarbTextBlock.Visibility = Visibility.Collapsed;
            FiberTextBlock.Visibility = Visibility.Collapsed;
            SugarTextBlock.Visibility = Visibility.Collapsed;
            SaltTextBlock.Visibility = Visibility.Collapsed;
            WaterTextBlock.Visibility = Visibility.Collapsed;
        }

        private async void InitializePageContentAsync()
        {
            try
            {
                // Создаем экземпляр FirestoreService
                var firestoreService = new FirestoreService();

                // Загрузка данных пользователя
                var userInfo = await GetUserInfoAsync(_currentUserCache.Id);

                if (userInfo == null || userInfo.Age == 0 || userInfo.Height == 0 || userInfo.Weight == 0)
                {
                    ShowUserInfoForm();
                }
                else
                {
                    ShowCalorieIntake(userInfo);

                    // Загрузка данных о приёмах пищи
                    var dailyMeal = await firestoreService.GetDailyMealsAsync(_currentUserCache.Id, DateTime.Now.ToString("yyyy-MM-dd"));

                    if (dailyMeal != null)
                    {
                        LoadMealsToUI(dailyMeal);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void LoadMealsToUI(DailyMealModel dailyMeal)
        {
            foreach (var mealType in dailyMeal.Meals.Keys)
            {
                var mealText = string.Join("\n", dailyMeal.Meals[mealType].Select(meal =>
                    $"{meal.Name}: {meal.Calories:0.0} ккал, Б: {meal.Proteins:0.0} г, Ж: {meal.Fats:0.0} г, В: {meal.Carbs:0.0} г"));

                switch (mealType)
                {
                    case "Сніданок":
                        BreakfastTextBlock.Text = mealText;
                        BreakfastTextBlock.Visibility = Visibility.Visible;
                        break;
                    case "Другий сніданок":
                        SecondBreakfastTextBlock.Text = mealText;
                        SecondBreakfastTextBlock.Visibility = Visibility.Visible;
                        break;
                    case "Обід":
                        LunchTextBlock.Text = mealText;
                        LunchTextBlock.Visibility = Visibility.Visible;
                        break;
                    case "Полудень":
                        AfternoonSnackTextBlock.Text = mealText;
                        AfternoonSnackTextBlock.Visibility = Visibility.Visible;
                        break;
                    case "Вечеря":
                        DinnerTextBlock.Text = mealText;
                        DinnerTextBlock.Visibility = Visibility.Visible;
                        break;
                    case "Друга вечеря":
                        SecondDinnerTextBlock.Text = mealText;
                        SecondDinnerTextBlock.Visibility = Visibility.Visible;
                        break;
                    case "Інший час":
                        if (OtherTimeTextBlock == null)
                        {
                            // Если элемент для отображения не создан, логируем
                            Console.WriteLine("Текстовый блок для 'Інший час' отсутствует.");
                        }
                        else
                        {
                            OtherTimeTextBlock.Text = mealText;
                            OtherTimeTextBlock.Visibility = Visibility.Visible;
                        }
                        break;
                }
            }
        }


        private async Task LoadDailyMeals(string userId, string date)
        {
            try
            {
                var firestoreService = new FirestoreService();
                var dailyMeal = await firestoreService.GetDailyMealsAsync(userId, date);

                if (dailyMeal != null)
                {
                    LoadMealsToUI(dailyMeal); // Здесь вызывается метод LoadMealsToUI
                }
                else
                {
                    Console.WriteLine("Нет данных для отображения.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_debounceTimer.IsEnabled)
                _debounceTimer.Stop(); // Останавливаем предыдущий таймер

            if (ProductSearchBox.Text.Length >= 2)
            {
                _debounceTimer.Interval = TimeSpan.FromMilliseconds(500); // Увеличиваем таймаут
                _debounceTimer.Start(); // Запускаем таймер для нового запроса
            }
            else
            {
                ProductSearchBox.ItemsSource = null;
                ProductSearchBox.IsDropDownOpen = false;
            }
        }


        private async void OnSearchTimerTick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            var products = await SearchProductsAsync(_lastQuery);
            ProductSearchBox.ItemsSource = products;
            ProductSearchBox.IsDropDownOpen = products.Count > 0;
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

        private async Task<List<(string Name, string Id)>> SearchProductsAsync(string query)
        {
            var products = new List<(string Name, string Id)>();
            try
            {
                using var client = new HttpClient();

                // Переводим запрос на английский
                var translator = new TranslatorService();
                string translatedQuery = await translator.TranslateTextAsync(query, "en");
                Console.WriteLine($"Переведенный запрос: {translatedQuery}");

                // Формируем URL для Spoonacular API
                string url = $"https://api.spoonacular.com/food/ingredients/search?query={Uri.EscapeDataString(translatedQuery)}&number=6&apiKey=77fc6d4be49f4522900362727af5549f";
                Console.WriteLine($"Запрос к Spoonacular: {url}");

                // Выполняем запрос
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка API Spoonacular: {response.StatusCode} - {errorDetails}");
                    return products;
                }

                var responseData = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseData);
                var resultsArray = json["results"];

                if (resultsArray != null)
                {
                    foreach (var result in resultsArray)
                    {
                        string name = result["name"]?.ToString();
                        string id = result["id"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(id))
                        {
                            // Переводим названия продуктов на украинский
                            string translatedName = await translator.TranslateTextAsync(name, "uk");
                            products.Add((translatedName, id));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка поиска продуктов: {ex.Message}");
            }

            return products;
        }

        private async void ProductSearchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedProductName = ProductSearchBox.SelectedItem as string;

            if (!string.IsNullOrEmpty(selectedProductName) && _productCache.Values.Any(v => v.Any(p => p.Name == selectedProductName)))
            {
                string productId = _productCache.Values
                    .SelectMany(v => v)
                    .FirstOrDefault(p => p.Name == selectedProductName).Id;

                // Проверяем, что поле количества заполнено корректно
                if (!int.TryParse(ProductQuantityTextBox.Text, out int quantity) || quantity <= 0)
                {
                    MessageBox.Show("Введите корректное количество продукта.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrEmpty(productId))
                {
                    try
                    {
                        // Передаем `quantity` в метод `GetProductDetailsAsync`
                        var productDetails = await GetProductDetailsAsync(productId, quantity);

                        if (productDetails != null)
                        {
                            // Обрабатывайте полученные данные
                            Console.WriteLine($"Данные продукта: {productDetails}");
                        }
                        else
                        {
                            MessageBox.Show("Не удалось получить данные о продукте.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка получения данных о продукте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }


        private async Task<dynamic> GetProductDetailsAsync(string productId, int amount)
        {
            try
            {
                using var client = new HttpClient();
                string url = $"https://api.spoonacular.com/food/ingredients/{productId}/information?amount={amount}&unit=grams&apiKey=77fc6d4be49f4522900362727af5549f";
                Console.WriteLine($"Запрос к API: {url}");

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка API Spoonacular: {response.StatusCode} - {errorDetails}");
                    return null;
                }

                var responseData = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseData);

                return new
                {
                    Calories = json["nutrition"]?["nutrients"]?.FirstOrDefault(n => n["name"]?.ToString() == "Calories")?["amount"]?.ToObject<double>() ?? 0,
                    Proteins = json["nutrition"]?["nutrients"]?.FirstOrDefault(n => n["name"]?.ToString() == "Protein")?["amount"]?.ToObject<double>() ?? 0,
                    Fats = json["nutrition"]?["nutrients"]?.FirstOrDefault(n => n["name"]?.ToString() == "Fat")?["amount"]?.ToObject<double>() ?? 0,
                    Carbs = json["nutrition"]?["nutrients"]?.FirstOrDefault(n => n["name"]?.ToString() == "Carbohydrates")?["amount"]?.ToObject<double>() ?? 0,
                    Fiber = json["nutrition"]?["nutrients"]?.FirstOrDefault(n => n["name"]?.ToString() == "Fiber")?["amount"]?.ToObject<double>() ?? 0,
                    Sugar = json["nutrition"]?["nutrients"]?.FirstOrDefault(n => n["name"]?.ToString() == "Sugar")?["amount"]?.ToObject<double>() ?? 0,
                    Salt = json["nutrition"]?["nutrients"]?.FirstOrDefault(n => n["name"]?.ToString() == "Sodium")?["amount"]?.ToObject<double>() ?? 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения деталей продукта: {ex.Message}");
                throw;
            }
        }



        private void UpdateFiberSugarSalt(double fiber, double sugar, double salt)
        {
            Console.WriteLine($"Обновляем данные: Клетчатка: {fiber}, Сахар: {sugar}, Соль: {salt}");

            // Если значения блоков пусты, задаем начальные значения
            if (string.IsNullOrWhiteSpace(FiberTextBlock.Text)) FiberTextBlock.Text = "Клітковина: 0 г";
            if (string.IsNullOrWhiteSpace(SugarTextBlock.Text)) SugarTextBlock.Text = "Цукор: 0 г";
            if (string.IsNullOrWhiteSpace(SaltTextBlock.Text)) SaltTextBlock.Text = "Сіль: 0 г";


            Console.WriteLine($"Текущие значения: {FiberTextBlock.Text}, {SugarTextBlock.Text}, {SaltTextBlock.Text}");
            // Обновляем данные
            double currentFiber = double.Parse(FiberTextBlock.Text.Split(':')[1].Trim().Split(' ')[0]);
            double currentSugar = double.Parse(SugarTextBlock.Text.Split(':')[1].Trim().Split(' ')[0]);
            double currentSalt = double.Parse(SaltTextBlock.Text.Split(':')[1].Trim().Split(' ')[0]);

            FiberTextBlock.Text = $"Клітковина: {currentFiber + fiber:0.0} г";
            SugarTextBlock.Text = $"Цукор: {currentSugar + sugar:0.0} г";
            SaltTextBlock.Text = $"Сіль: {currentSalt + salt:0.0} г";

            // Делаем блоки видимыми
            FiberTextBlock.Visibility = Visibility.Visible;
            SugarTextBlock.Visibility = Visibility.Visible;
            SaltTextBlock.Visibility = Visibility.Visible;
        }


        private bool IsDrink(string productName)
        {
            string[] drinks = { "вода", "чай", "сок", "кофе", "молоко" }; // Добавьте свои варианты
            return drinks.Any(d => productName.ToLower().Contains(d));
        }


        private void UpdateWaterIntake(int quantity)
        {
            double currentWater = 0;

            if (!string.IsNullOrWhiteSpace(WaterTextBlock.Text))
            {
                currentWater = double.Parse(WaterTextBlock.Text.Split(':')[1].Trim().Split(' ')[0]);
            }

            double totalWater = currentWater + (quantity / 1000.0); // Переводим в литры
            WaterTextBlock.Text = $"Питний режим: {totalWater:0.0} л";

            // Убедитесь, что элемент видим
            WaterTextBlock.Visibility = Visibility.Visible;
        }

        private async void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductSearchBox.SelectedItem is null || !int.TryParse(ProductQuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Выберите продукт из списка и введите корректное количество.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedProduct = _productCache.Values
                .SelectMany(v => v)
                .FirstOrDefault(p => p.Name == ProductSearchBox.Text);

            if (selectedProduct == default)
            {
                MessageBox.Show("Продукт не найден. Пожалуйста, выберите из выпадающего списка.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string productId = selectedProduct.Id;
            string productName = selectedProduct.Name;

            try
            {
                var productDetails = await GetProductDetailsAsync(productId, quantity);

                if (productDetails != null)
                {
                    double factor = quantity / 100.0;
                    double calories = productDetails.Calories * factor;
                    double proteins = productDetails.Proteins * factor;
                    double fats = productDetails.Fats * factor;
                    double carbs = productDetails.Carbs * factor;

                    var mealType = DetermineMealType();

                    var mealItem = new MealItem
                    {
                        Name = productName,
                        Calories = calories,
                        Proteins = proteins,
                        Fats = fats,
                        Carbs = carbs
                    };

                    // Сохранение в Firestore
                    await AddMealToFirestore(mealType, _currentUserCache.Id, DateTime.Now.ToString("yyyy-MM-dd"), mealItem);

                    // Отображение в UI
                    UpdateMealDetails(mealType, productName, calories, proteins, fats, carbs);
                }
                else
                {
                    MessageBox.Show("Не удалось получить данные о продукте.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления продукта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void UpdateTotalNutrition(double calories, double proteins, double fats, double carbs)
        {
            try
            {
                // Получаем текущие значения из текстовых блоков и обрабатываем их
                double currentCalories = ParseDoubleOrDefault(CalorieTextBlock.Text.Split('/')[0].Trim(), 0);
                double currentProteins = ParseDoubleOrDefault(ProteinTextBlock.Text.Split(':')[1].Trim().Split('/')[0].Trim(), 0);
                double currentFats = ParseDoubleOrDefault(FatTextBlock.Text.Split(':')[1].Trim().Split('/')[0].Trim(), 0);
                double currentCarbs = ParseDoubleOrDefault(CarbTextBlock.Text.Split(':')[1].Trim().Split('/')[0].Trim(), 0);

                // Обновляем значения
                double totalCalories = currentCalories + calories;
                double totalProteins = currentProteins + proteins;
                double totalFats = currentFats + fats;
                double totalCarbs = currentCarbs + carbs;

                // Обновляем текстовые блоки
                CalorieTextBlock.Text = $"{totalCalories:0} / {CalculateDailyCalorieIntake(ConvertToUserInfoModel(_currentUserCache, null)):0} калорій";
                ProteinTextBlock.Text = $"Білки: {totalProteins:0} г";
                FatTextBlock.Text = $"Жири: {totalFats:0} г";
                CarbTextBlock.Text = $"Вуглеводи: {totalCarbs:0} г";

                // Делаем текстовые блоки видимыми, если они скрыты
                CalorieTextBlock.Visibility = Visibility.Visible;
                ProteinTextBlock.Visibility = Visibility.Visible;
                FatTextBlock.Visibility = Visibility.Visible;
                CarbTextBlock.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Вспомогательный метод для безопасного парсинга строки в double
        private double ParseDoubleOrDefault(string input, double defaultValue)
        {
            if (double.TryParse(input, out double result))
            {
                return result;
            }
            return defaultValue;
        }

        private void UpdateMealDetails(string mealType, string productName, double calories, double proteins, double fats, double carbs)
        {
            string mealText = $"{productName}: {calories:0.0} ккал, Б: {proteins:0.0} г, Ж: {fats:0.0} г, В: {carbs:0.0} г";

            switch (mealType)
            {
                case "Breakfast":
                    BreakfastTextBlock.Text += $"{mealText}\n";
                    break;
                case "SecondBreakfast":
                    SecondBreakfastTextBlock.Text += $"{mealText}\n";
                    break;
                case "Lunch":
                    LunchTextBlock.Text += $"{mealText}\n";
                    break;
                case "AfternoonSnack":
                    AfternoonSnackTextBlock.Text += $"{mealText}\n";
                    break;
                case "Dinner":
                    DinnerTextBlock.Text += $"{mealText}\n";
                    break;
                case "SecondDinner":
                    SecondDinnerTextBlock.Text += $"{mealText}\n";
                    break;
            }
        }

        public async Task AddMealToFirestore(string mealType, string userId, string date, MealItem mealItem)
        {
            try
            {
                var docRef = _firestoreDb.Collection("DailyMeals").Document($"{userId}_{date}");
                var snapshot = await docRef.GetSnapshotAsync();

                DailyMealModel dailyMeal;

                if (snapshot.Exists)
                {
                    dailyMeal = snapshot.ConvertTo<DailyMealModel>();
                }
                else
                {
                    dailyMeal = new DailyMealModel
                    {
                        UserId = userId,
                        Date = date,
                        Meals = new Dictionary<string, List<MealItem>>()
                    };
                }

                if (!dailyMeal.Meals.ContainsKey(mealType))
                {
                    dailyMeal.Meals[mealType] = new List<MealItem>();
                }

                dailyMeal.Meals[mealType].Add(mealItem);

                await docRef.SetAsync(dailyMeal);
                Console.WriteLine("Данные успешно добавлены.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения данных в Firestore: {ex.Message}");
                throw;
            }
        }



        private void ShowUserInfoForm()
        {
            Console.WriteLine("Показ формы ввода данных пользователя.");
            var userInfoForm = new UserInfoForm(_currentUserCache); // Создаём форму
            UserInfoFormContainer.Content = userInfoForm; // Добавляем в контейнер

            UserInfoFormContainer.Visibility = Visibility.Visible; // Делаем контейнер видимым
            CaloriePageContainer.Visibility = Visibility.Collapsed; // Скрываем контейнер с калориями

            Console.WriteLine("Форма ввода данных отображена.");
        }

        private void ShowCalorieIntake(UserInfoModel userInfo)
        {
            double dailyCalories = CalculateDailyCalorieIntake(userInfo);

            // Рассчитываем норму воды (предположим, 30 минут активности)
            double dailyWaterIntake = CalculateWaterIntake(userInfo, activityMinutes: 30);

            // Отображаем калорийность
            CalorieTextBlock.Text = $"0 / {dailyCalories:0} калорій";
            CalorieTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
            CalorieTextBlock.VerticalAlignment = VerticalAlignment.Top;
            CalorieTextBlock.Visibility = Visibility.Visible;

            // Обновляем блоки с белками, жирами, углеводами
            ProteinTextBlock.Text = $"Білки: 0 г";
            FatTextBlock.Text = $"Жири: 0 г";
            CarbTextBlock.Text = $"Вуглеводи: 0 г";

            // Обновляем другие текстовые блоки (если требуется)
            FiberTextBlock.Text = $"Клітковина: 0 г";
            SugarTextBlock.Text = $"Цукор: 0 г";
            SaltTextBlock.Text = $"Сіль: 0 г";

            // Отображаем элементы страницы калорий
            CaloriePageContainer.Visibility = Visibility.Visible;
            UserInfoFormContainer.Visibility = Visibility.Collapsed;

            // Расположение верхней части (если требуется)
            CalorieTextBlock.Margin = new Thickness(0, 20, 0, 0); // Отступ сверху
            CalorieTextBlock.FontSize = 18; // Увеличиваем шрифт для большей видимости
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
                bmr = 66 + (13.7 * weight) + (5 * height) - (6.8 * age);
            }
            else
            {
                bmr = 655 + (9.6 * weight) + (1.8 * height) - (4.7 * age);
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
                "Схуднення" => (0.25, 0.225, 0.475),
                "Набір маси" => (0.175, 0.275, 0.55),
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

        private string DetermineMealType()
        {
            var currentTime = DateTime.Now.TimeOfDay;

            if (currentTime >= TimeSpan.FromHours(6) && currentTime < TimeSpan.FromHours(9))
                return "Сніданок";
            if (currentTime >= TimeSpan.FromHours(9) && currentTime < TimeSpan.FromHours(11))
                return "Другий сніданок";
            if (currentTime >= TimeSpan.FromHours(12) && currentTime < TimeSpan.FromHours(14))
                return "Обід";
            if (currentTime >= TimeSpan.FromHours(15) && currentTime < TimeSpan.FromHours(16))
                return "Полудень";
            if (currentTime >= TimeSpan.FromHours(18) && currentTime < TimeSpan.FromHours(20))
                return "Вечеря";
            if (currentTime >= TimeSpan.FromHours(20) && currentTime < TimeSpan.FromHours(22))
                return "Друга вечеря";

            return "Інший час";
        }



        private UserInfoModel ConvertToUserInfoModel(UserModel userModel, UserInfoModel userInfo)
        {
            return new UserInfoModel
            {
                UserId = userModel.Id,
                Gender = userInfo?.Gender ?? string.Empty,
                Age = userInfo?.Age ?? 0,
                Height = userInfo?.Height ?? 0,
                Weight = userInfo?.Weight ?? 0,
                ActivityLevel = userInfo?.ActivityLevel ?? string.Empty,
                Purpose = userInfo?.Purpose ?? string.Empty
            };
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
