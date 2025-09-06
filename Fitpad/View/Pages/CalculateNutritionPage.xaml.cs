using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fitpad.ViewModel.PagesViewModels;
using Fitpad.Model.Entities;
using Fitpad.Services;
using Google.Cloud.Firestore;
using System.Threading.Tasks;
using Fitpad.View.Components;
using System.Collections.Generic;


namespace Fitpad.View.Pages
{
    public partial class CalculateNutritionPage : Page
    {
        private readonly CalculateNutritionViewModel _viewModel;
        private readonly TranslatorService _translatorService;
        private readonly FirestoreDb _firestoreDb;
        private static string _currentUserId = string.Empty;

        private static CalculateNutritionPage _instance; // Экземпляр Singleton
        private static readonly object _lock = new object(); // Объект блокировки

        private bool _isCalculatorEnabled = false;

        private string _manualEntryProductName;
        private double _manualEntryWeight;


        public CalculateNutritionPage() : this(new UserInfoModel()) { }

        private CalculateNutritionPage(UserInfoModel userInfo)
        {
            InitializeComponent();
            _translatorService = new TranslatorService();
            var firestoreService = new FirestoreService();
            _firestoreDb = firestoreService.GetFirestoreDb();

            if (userInfo == null)
            {
                Console.WriteLine("❌ Ошибка: данные пользователя отсутствуют.");
                userInfo = new UserInfoModel();
            }

            _viewModel = new CalculateNutritionViewModel(userInfo);
            DataContext = _viewModel;
            _viewModel.ShowManualEntryOverlayAction = ShowManualEntryOverlay;

            LoadUserProducts(); // Загружаем сохраненные продукты

            // Проверяем пользователя
            CheckUserAndUpdateData();

            if (userInfo == null)
            {
                MessageBox.Show("Помилка: дані користувача відсутні!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                userInfo = new UserInfoModel();
            }

            _viewModel = new CalculateNutritionViewModel(userInfo);
            DataContext = _viewModel;
            _viewModel.ShowManualEntryOverlayAction = ShowManualEntryOverlay;

            Console.WriteLine("📊 DataContext установлен!");

            // 🔥 Вызываем обновление диаграмм после загрузки данных
            DelayAndUpdateUI();
        }

        private async void DelayAndUpdateUI()
        {
            await Task.Delay(300); // Задержка 100 мс
            Console.WriteLine("⏳ 100 мс прошло, обновляем диаграммы...");

            _viewModel.UpdatePieChart();
            UpdateCalorieDisplay();

        }

        private void UpdateCalorieDisplay(double? customCalories = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (CalorieIntakeText != null)
                {
                    double newCalories = customCalories ?? _viewModel.CurrentCalories;
                    double dailyCalorieNorm = _viewModel.CalorieNorm;
                    Console.WriteLine($"🔍 Перед обновлением: CurrentCalories = {_viewModel.CurrentCalories}");

                    CalorieIntakeText.Text = $"Ккал: {newCalories:0.0} / {dailyCalorieNorm:0.0}";

                    ProteinDisplayText.Text = $"Білки: {_viewModel.CurrentProtein:0.0} / 80 г";
                    FatsDisplayText.Text = $"Жири: {_viewModel.CurrentFats:0.0} / 45 г";
                    CarbsDisplayText.Text = $"Вуглеводи: {_viewModel.CurrentCarbs:0.0} / 220 г";
                    WaterDisplayText.Text = $"Вода: {_viewModel.CurrentWater:0.0} / 2000 мл";

                    Console.WriteLine($"🔹 Обновлены данные: Калории {newCalories}, БЖУ {ProteinDisplayText.Text}, {FatsDisplayText.Text}, {CarbsDisplayText.Text}");
                }
            });
        }


        public static CalculateNutritionPage GetInstance(UserInfoModel userInfo)
        {
            lock (_lock) // Защищаем от многопоточного доступа
            {
                if (_instance == null || _currentUserId != userInfo.UserId)
                {
                    _currentUserId = userInfo.UserId;
                    _instance = new CalculateNutritionPage(userInfo);
                }
                return _instance;
            }
        }

        // ✅ Метод, который вызывается перед открытием страницы
        private static bool _isProcessing = false; // 🔒 Защита от повторного вызова

        public static async Task<bool> GetInstanceWithCheck()
        {
            if (_isProcessing) return false;

            _isProcessing = true;
            string userId = UserSession.CurrentUserId;

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("❌ Ошибка: пользователь не найден.");
                MessageBox.Show("Вийдіть в акаунт", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                _isProcessing = false;
                return false;
            }

            var userInfo = await GetUserInfoAsync(userId);

            if (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0)
            {
                Console.WriteLine("❌ Ошибка: данные пользователя отсутствуют.");
                MessageBox.Show("Вийдіть в акаунт", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                _isProcessing = false;
                return false;
            }

            // ✅ Данные заполнены, открываем калькулятор
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainViewModel.Instance.CurrentPage = GetInstance(userInfo);
            });

            _isProcessing = false;
            return true;
        }


        private async void OpenCalculator_Click(object sender, RoutedEventArgs e)
        {
            bool isOpened = await CalculateNutritionPage.GetInstanceWithCheck();

            if (!isOpened)
            {
                Console.WriteLine("⛔ Открытие калькулятора заблокировано: сначала нужно заполнить анкету!");
            }
        }



        private async void CheckUserAndUpdateData()
        {
            string newUserId = GetCurrentUserId();

            if (_currentUserId != newUserId)
            {
                _currentUserId = newUserId;
                await UpdateUserNutritionData(newUserId);
            }
        }

        private async Task UpdateUserNutritionData(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("❌ Ошибка: UserId пустой!");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Вийдіть у свій акаунт", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            var userInfo = await GetUserInfoAsync(userId);

            if (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0)
            {
                Console.WriteLine("❌ Данные пользователя отсутствуют или некорректны.");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Увійдіть у свій акаунт", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                });

                return; // Остановить выполнение, если данные некорректны
            }

            // ✅ Данные корректны, продолжаем обновление информации
            _viewModel.CalorieNorm = _viewModel.CalculateDailyCalorieIntake(userInfo);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (CalorieIntakeText != null)
                {
                    CalorieIntakeText.Text = $"Ккал: {_viewModel.CurrentCalories:0.0} / {_viewModel.CalorieNorm:0.0}";
                    Console.WriteLine($"🔥 Обновлён UI: {CalorieIntakeText.Text}");
                }
            });

            Console.WriteLine("✅ Данные пользователя обновлены, калькулятор готов к работе.");
        }


        private static async Task<UserInfoModel> GetUserInfoAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("❌ Ошибка: UserId пустой или неопределённый!");
                return null;
            }

            try
            {
                var firestoreDb = new FirestoreService().GetFirestoreDb();
                var userInfoDoc = await firestoreDb.Collection("UserInfos").Document(userId).GetSnapshotAsync();

                if (!userInfoDoc.Exists)
                {
                    Console.WriteLine("❌ Ошибка: Данные пользователя не найдены в Firestore!");
                    return null;
                }

                return userInfoDoc.ConvertTo<UserInfoModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при получении данных пользователя: {ex.Message}");
                return null;
            }
        }


        private string GetCurrentUserId()
        {
            return UserSession.CurrentUserId;
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

        private void ShowManualEntryOverlay(string productName, double weight)
        {
            ManualEntryOverlay.Visibility = Visibility.Visible;
            CaloriesInput.Text = "";
            ProteinInput.Text = "";
            FatsInput.Text = "";
            CarbsInput.Text = "";

            _manualEntryProductName = productName;
            _manualEntryWeight = weight;
        }

        private void HideManualEntryOverlay()
        {
            ManualEntryOverlay.Visibility = Visibility.Collapsed;
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
            if (_translatorService == null)
            {
                Console.WriteLine("❌ Ошибка: _translatorService не инициализирован!");
                MessageBox.Show("Помилка: сервіс перекладу недоступний!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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

            Console.WriteLine($"🔹 Введено пользователем: {productName}, Вес: {weight} г");

            // Перевод названия продукта
            string translatedName = await _translatorService.TranslateTextAsync(productName, "en");
            if (string.IsNullOrWhiteSpace(translatedName))
            {
                MessageBox.Show("Ошибка перевода названия продукта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Console.WriteLine($"🔹 Переведено: {translatedName}");

            // Поиск в OpenFoodFacts API
            var product = await _viewModel.SearchAndAddProductAsync(translatedName, weight);
            if (product != null)
            {
                AddProductToTable(product);
            }
            else
            {
                Console.WriteLine("❌ Продукт НЕ НАЙДЕН в OpenFoodFacts API!");
            }
            UpdateCalorieDisplay();
        }


        private async void OnManualEntryConfirm(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(CaloriesInput.Text, out double calories) ||
                !double.TryParse(ProteinInput.Text, out double protein) ||
                !double.TryParse(FatsInput.Text, out double fats) ||
                !double.TryParse(CarbsInput.Text, out double carbs))
            {
                MessageBox.Show("Будь ласка, введіть коректні числові значення!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var manualProduct = new NutritionModel
            {
                Title = _manualEntryProductName,
                Weight = _manualEntryWeight,
                Calories = calories,
                Protein = protein,
                Fats = fats,
                Carbs = carbs,
                Time = DateTime.Now.ToString("HH:mm")
            };

            Console.WriteLine($"✅ Вручную добавлен продукт: {manualProduct.Title}, калорії: {manualProduct.Calories}");

            // 🔹 Автоматически добавляем в таблицу и сохраняем в Firestore
            AddProductToTable(manualProduct);

            ForceUpdateCharts();
     
            HideManualEntryOverlay();
        }

        private async void AddProductToTable(NutritionModel product)
        {
            if (product == null) return;

            var userId = UserSession.CurrentUserId;
            var fs = new FirestoreService();

            // 1) Дневник: факт употребления (обязательно)
            await fs.AddFoodDiaryEntryAsync(userId, product, DateTime.UtcNow);

            // 2) Каталог: опционально, чтобы «новые» попадали в UserProducts без дублей
            await fs.SaveUserProductAsync(userId, product);


            // 3) UI / суммы
            _viewModel.SavedProducts.Add(product);
            _viewModel.CurrentCalories += product.Calories;
            _viewModel.CurrentProtein += product.Protein;
            _viewModel.CurrentFats += product.Fats;
            _viewModel.CurrentCarbs += product.Carbs;
            if ((product.Title ?? "").ToLower().Contains("вода"))
                _viewModel.CurrentWater += product.Weight;

            _viewModel.UpdatePieChart();
            UpdateCalorieDisplay();
        }



        private void OnManualEntryCancel(object sender, RoutedEventArgs e)
        {
            HideManualEntryOverlay();
        }


        public double CalculateDailyCalorieIntake(UserInfoModel userInfo)
        {
            if (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0)
            {
                Console.WriteLine("⚠ Ошибка: Некорректные пользовательские данные!");
                return 2000; 
            }

            double bmr;
            if (userInfo.Gender == "Чоловік" || userInfo.Gender == "Мужчина")
            {
                bmr = 88.36 + (13.4 * userInfo.Weight) + (4.8 * userInfo.Height) - (5.7 * userInfo.Age);
            }
            else
            {
                bmr = 447.6 + (9.2 * userInfo.Weight) + (3.1 * userInfo.Height) - (4.3 * userInfo.Age);
            }

            double activityMultiplier = userInfo.ActivityLevel switch
            {
                "Низька" => 1.2,
                "Середня" => 1.375,
                "Висока" => 1.55,
                "Дуже висока" => 1.725,
                "Екстремальна" => 1.9,
                _ => 1.2
            };

            double tdee = bmr * activityMultiplier;

            tdee = userInfo.Purpose switch
            {
                "Схуднення" => tdee * 0.85,
                "Набір маси" => tdee * 1.15,
                _ => tdee
            };

            return Math.Round(tdee, 1);
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && (textBox.Text == "Введіть калорії..." || textBox.Text == "Введіть білки..." ||
                                    textBox.Text == "Введіть жири..." || textBox.Text == "Введіть вуглеводи..."))
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black;
            }
        }

        private void OnInputFieldsChanged(object sender, TextChangedEventArgs e)
        {
            // Проверяем, что все элементы существуют, чтобы избежать NullReferenceException
            if (OkButton == null || CaloriesInput == null || ProteinInput == null ||
                FatsInput == null || CarbsInput == null)
            {
                return;
            }

            // Активация кнопки, если все поля заполнены
            OkButton.IsEnabled = !string.IsNullOrWhiteSpace(CaloriesInput.Text) &&
                                 !string.IsNullOrWhiteSpace(ProteinInput.Text) &&
                                 !string.IsNullOrWhiteSpace(FatsInput.Text) &&
                                 !string.IsNullOrWhiteSpace(CarbsInput.Text);
        }

        private async void LoadUserProducts()
        {
            if (_viewModel == null)
            {
                Console.WriteLine("❌ Ошибка: _viewModel не инициализирован!");
                return;
            }

            var firestoreService = new FirestoreService();
            var today = DateTime.Now;
            var products = await firestoreService.GetFoodDiaryForDateAsync(UserSession.CurrentUserId, today);

            if (products == null || products.Count == 0)
            {
                Console.WriteLine($"⚠ Продукты не найдены для пользователя {UserSession.CurrentUserId}");
                ForceUpdateCharts();
                return;
            }

            Console.WriteLine($"✅ Загружено {products.Count} продуктов для пользователя {UserSession.CurrentUserId}");

            _viewModel.SavedProducts.Clear();
            _viewModel.CurrentCalories = 0;
            _viewModel.CurrentProtein = 0;
            _viewModel.CurrentFats = 0;
            _viewModel.CurrentCarbs = 0;
            _viewModel.CurrentWater = 0;

            foreach (var product in products)
            {
                _viewModel.SavedProducts.Add(product);

                _viewModel.CurrentCalories += product.Calories;
                _viewModel.CurrentProtein += product.Protein;
                _viewModel.CurrentFats += product.Fats;
                _viewModel.CurrentCarbs += product.Carbs;

                if (product.Title.ToLower().Contains("вода"))
                {
                    _viewModel.CurrentWater += product.Weight;
                }

                Console.WriteLine($"🟢 {product.Title}: {product.Calories} ккал добавлено");

            }

            await Task.Delay(200);
            ForceUpdateCharts();
        }

        private void ForceUpdateCharts()
        {
            Console.WriteLine("🔄 Принудительное обновление диаграмм и UI...");

            Application.Current.Dispatcher.Invoke(() =>
            {
                _viewModel.UpdatePieChart();
                UpdateCalorieDisplay();
            });

            Console.WriteLine("✅ Диаграммы и UI обновлены!");
        }


        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NutritionModel product)
            {
                // Подтверждение удаления
                MessageBoxResult result = MessageBox.Show($"Видалити '{product.Title}'?", "Підтвердження",
                                                          MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Удаляем из Firestore
                    await DeleteProductFromFirestore(product);

                    // Удаляем из UI
                    _viewModel.SavedProducts.Remove(product);
                    Console.WriteLine($"🗑 Видалено з UI: {product.Title}");

                    // Обновляем текущие значения
                    _viewModel.CurrentCalories -= product.Calories;
                    _viewModel.CurrentProtein -= product.Protein;
                    _viewModel.CurrentFats -= product.Fats;
                    _viewModel.CurrentCarbs -= product.Carbs;

                    if (product.Title.ToLower().Contains("вода"))
                    {
                        _viewModel.CurrentWater -= product.Weight;
                    }
                    Console.WriteLine($"🗑 Продукт '{product.Title}' удален.");
                    ForceUpdateCharts();
                }
            }
        }

        private async Task DeleteProductFromFirestore(NutritionModel product)
        {
            try
            {
                string userId = UserSession.CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("❌ [Firestore] Помилка: UserID не знайдено.");
                    return;
                }

                var db = FirestoreDb.Create("fitpad-2025");
                var diaryRef = db.Collection("Users").Document(userId).Collection("FoodDiary");

                // Если ты показываешь текущий день — возьмём локальную дату сегодня.
                // Если в UI выбирается дата — подставь её сюда.
                string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
                string timeStr = product.Time ?? "";

                var query = diaryRef
                    .WhereEqualTo("Date", dateStr)
                    .WhereEqualTo("Title", product.Title ?? "")
                    .WhereEqualTo("Weight", product.Weight)
                    .WhereEqualTo("Calories", product.Calories);

                // Если поле Time заполняешь и отображаешь — добавь и его в фильтр:
                if (!string.IsNullOrEmpty(timeStr))
                    query = query.WhereEqualTo("Time", timeStr);

                var snap = await query.GetSnapshotAsync();

                if (snap.Count == 0)
                {
                    Console.WriteLine("⚠ Запись в FoodDiary не найдена по указанным полям.");
                    return;
                }

                foreach (var doc in snap.Documents)
                {
                    await doc.Reference.DeleteAsync();
                    Console.WriteLine($"✅ [Firestore] Видалено запис FoodDiary: {doc.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Firestore] Помилка при видаленні запису FoodDiary: {ex.Message}");
            }
        }


        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox.Name == "CaloriesInput") textBox.Text = "Введіть калорії...";
                if (textBox.Name == "ProteinInput") textBox.Text = "Введіть білки...";
                if (textBox.Name == "FatsInput") textBox.Text = "Введіть жири...";
                if (textBox.Name == "CarbsInput") textBox.Text = "Введіть вуглеводи...";

                textBox.Foreground = Brushes.Gray;
            }
        }


    }
}

