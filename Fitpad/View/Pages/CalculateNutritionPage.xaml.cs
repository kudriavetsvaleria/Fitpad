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

namespace Fitpad.View.Pages
{
    public partial class CalculateNutritionPage : Page
    {
        private readonly CalculateNutritionViewModel _viewModel;
        private readonly TranslatorService _translatorService;
        private readonly FirestoreDb _firestoreDb;
        private static string _currentUserId = string.Empty;

        private static CalculateNutritionPage _instance;
        private static readonly object _lock = new object();

        private string _manualEntryProductName;
        private double _manualEntryWeight;

        public CalculateNutritionPage() : this(new UserInfoModel()) { }

        private CalculateNutritionPage(UserInfoModel userInfo)
        {
            InitializeComponent();

            _translatorService = new TranslatorService();
            var firestoreService = new FirestoreService();
            _firestoreDb = firestoreService.GetFirestoreDb();

            if (userInfo == null) userInfo = new UserInfoModel();

            _viewModel = new CalculateNutritionViewModel(userInfo);
            DataContext = _viewModel;
            _viewModel.ShowManualEntryOverlayAction = ShowManualEntryOverlay;

            LoadUserProducts();
            CheckUserAndUpdateData();

            // один-единственный DataContext — без второго new!
            DelayAndUpdateUI();
        }

        private async void DelayAndUpdateUI()
        {
            await Task.Delay(300);
            _viewModel.UpdatePieChart();
            UpdateCalorieDisplay();
        }

        private void UpdateCalorieDisplay(double? customCalories = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (CalorieIntakeText == null) return;

                double newCalories = customCalories ?? _viewModel.CurrentCalories;
                double dailyCalorieNorm = _viewModel.CalorieNorm;

                CalorieIntakeText.Text = $"Ккал: {newCalories:0.0} / {dailyCalorieNorm:0.0}";
                ProteinDisplayText.Text = $"Білки: {_viewModel.CurrentProtein:0.0} / 80 г";
                FatsDisplayText.Text = $"Жири: {_viewModel.CurrentFats:0.0} / 45 г";
                CarbsDisplayText.Text = $"Вуглеводи: {_viewModel.CurrentCarbs:0.0} / 220 г";
                WaterDisplayText.Text = $"Вода: {_viewModel.CurrentWater:0.0} / 2000 мл";
            });
        }

        public static CalculateNutritionPage GetInstance(UserInfoModel userInfo)
        {
            lock (_lock)
            {
                if (_instance == null || _currentUserId != userInfo.UserId)
                {
                    _currentUserId = userInfo.UserId;
                    _instance = new CalculateNutritionPage(userInfo);
                }
                return _instance;
            }
        }

        private static bool _isProcessing = false;
        public static async Task<bool> GetInstanceWithCheck()
        {
            if (_isProcessing) return false;
            _isProcessing = true;

            string userId = UserSession.CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                MessageBox.Show("Вийдіть в акаунт", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                _isProcessing = false;
                return false;
            }

            var userInfo = await GetUserInfoAsync(userId);
            if (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0)
            {
                MessageBox.Show("Вийдіть в акаунт", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                _isProcessing = false;
                return false;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                MainViewModel.Instance.CurrentPage = GetInstance(userInfo);
            });

            _isProcessing = false;
            return true;
        }

        private async void OpenCalculator_Click(object sender, RoutedEventArgs e)
        {
            await GetInstanceWithCheck();
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
                MessageBox.Show("Вийдіть у свій акаунт", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var userInfo = await GetUserInfoAsync(userId);
            if (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0)
            {
                MessageBox.Show("Увійдіть у свій акаунт", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _viewModel.CalorieNorm = _viewModel.CalculateDailyCalorieIntake(userInfo);
            UpdateCalorieDisplay();
        }

        private static async Task<UserInfoModel> GetUserInfoAsync(string userId)
        {
            try
            {
                var firestoreDb = new FirestoreService().GetFirestoreDb();
                var userInfoDoc = await firestoreDb.Collection("UserInfos").Document(userId).GetSnapshotAsync();
                return userInfoDoc.Exists ? userInfoDoc.ConvertTo<UserInfoModel>() : null;
            }
            catch
            {
                return null;
            }
        }

        private string GetCurrentUserId() => UserSession.CurrentUserId;

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb &&
               (tb.Text == "Введите название продукта..." || tb.Text == "Назва продукту..."))
            {
                tb.Text = "";
                tb.Foreground = Brushes.Black;
            }
        }
        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = "Назва продукту...";
                tb.Foreground = Brushes.Gray;
            }
        }

        private void ShowManualEntryOverlay(string productName, double weight)
        {
            ManualEntryOverlay.Visibility = Visibility.Visible;
            CaloriesInput.Text = ProteinInput.Text = FatsInput.Text = CarbsInput.Text = "";

            _manualEntryProductName = productName;
            _manualEntryWeight = weight;
        }

        private void HideManualEntryOverlay() => ManualEntryOverlay.Visibility = Visibility.Collapsed;

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
        private static string NormalizeTitleUk(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            s = s.Trim();
            // Первая буква — заглавная, остальное как есть
            return char.ToUpper(s[0]) + (s.Length > 1 ? s.Substring(1) : "");
        }

        private static bool HasLatin(string s) =>
    !string.IsNullOrEmpty(s) && s.Any(c => c <= 127 && char.IsLetter(c));

        private async Task MigrateTodayTitlesToUkrAsync()
        {
            var fs = new FirestoreService();
            var today = DateTime.Now;
            var items = await fs.GetFoodDiaryForDateAsync(UserSession.CurrentUserId, today);

            foreach (var item in items)
            {
                if (HasLatin(item.Title))
                {
                    try
                    {
                        var uk = await _translatorService.TranslateTextAsync(item.Title, "uk");
                        if (!string.IsNullOrWhiteSpace(uk) && uk != item.Title)
                        {
                            item.Title = NormalizeTitleUk(uk);
                            // реализуй у себя обновление названия в дневнике за сегодня:
                            await fs.UpdateFoodDiaryEntryTitleAsync(UserSession.CurrentUserId, today, item, item.Title, false);

                        }
                    }
                    catch { /* игнорим единичные провалы */ }
                }
            }
        }


        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_translatorService == null)
            {
                MessageBox.Show("Помилка: сервіс перекладу недоступний!", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string productName = SearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(productName) || productName == "Назва продукту...")
            {
                MessageBox.Show("Введіть назву продукту!", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(WeightBox.Text.Trim(), out double weight) || weight <= 0)
            {
                MessageBox.Show("Будь ласка, введіть коректну вагу!", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1) переводим укр → англ для запроса к API
            string translatedName = await _translatorService.TranslateTextAsync(productName, "en");

            // 2) ищем продукт по API
            var product = await _viewModel.SearchAndAddProductAsync(translatedName, weight);

            if (product != null)
            {
                // 3) переводим название продукта из API обратно на укр (для отображения и БД)
                try
                {
                    var ukTitle = await _translatorService.TranslateTextAsync(product.Title ?? productName, "uk");
                    if (string.IsNullOrWhiteSpace(ukTitle)) ukTitle = productName;
                    product.Title = NormalizeTitleUk(ukTitle);
                }
                catch
                {
                    product.Title = NormalizeTitleUk(productName);
                }

                AddProductToTable(product);   // ← в БД/таблицу уйдёт украинское название
                UpdateCalorieDisplay();
            }
            else
            {
                MessageBox.Show($"Не знайдено інформації про продукт '{productName}'. Введіть дані вручну.",
                                "Ручне додавання", MessageBoxButton.OK, MessageBoxImage.Information);

                try
                {
                    var dialog = new Fitpad.View.ManualProductEntryDialog(productName, weight);
                    var ownerWindow = Window.GetWindow(this);
                    if (ownerWindow != null && ownerWindow.IsVisible) dialog.Owner = ownerWindow;

                    bool? result = dialog.ShowDialog();
                    if (result == true && dialog.CreatedProduct != null)
                    {
                        AddProductToTable(dialog.CreatedProduct);
                        UpdateCalorieDisplay();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка при відкритті форми: {ex.Message}",
                                    "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

            AddProductToTable(manualProduct);
            ForceUpdateCharts();
            HideManualEntryOverlay();
        }

        private async void AddProductToTable(NutritionModel product)
        {
            if (product == null) return;

            var userId = UserSession.CurrentUserId;
            var fs = new FirestoreService();

            await fs.AddFoodDiaryEntryAsync(userId, product, DateTime.UtcNow);
            await fs.SaveUserProductAsync(userId, product);

            _viewModel.SavedProducts.Add(product);
            _viewModel.CurrentCalories += product.Calories;
            _viewModel.CurrentProtein += product.Protein;
            _viewModel.CurrentFats += product.Fats;
            _viewModel.CurrentCarbs += product.Carbs;
            if ((product.Title ?? "").ToLower().Contains("вода"))
                _viewModel.CurrentWater += product.Weight;

            _viewModel.UpdatePieChart();
            UpdateCalorieDisplay();

            // 🔥 пересчитать дневную сводку на сегодня
            await fs.RecomputeTodayAsync(userId);
        }


        private void OnManualEntryCancel(object sender, RoutedEventArgs e) => HideManualEntryOverlay();

        private void OnInputFieldsChanged(object sender, TextChangedEventArgs e)
        {
            if (OkButton == null || CaloriesInput == null || ProteinInput == null || FatsInput == null || CarbsInput == null) return;

            OkButton.IsEnabled =
                !string.IsNullOrWhiteSpace(CaloriesInput.Text) &&
                !string.IsNullOrWhiteSpace(ProteinInput.Text) &&
                !string.IsNullOrWhiteSpace(FatsInput.Text) &&
                !string.IsNullOrWhiteSpace(CarbsInput.Text);
        }

        private async void LoadUserProducts()
        {
            var firestoreService = new FirestoreService();
            var today = DateTime.Now;
            var products = await firestoreService.GetFoodDiaryForDateAsync(UserSession.CurrentUserId, today);

            _viewModel.SavedProducts.Clear();
            _viewModel.CurrentCalories = _viewModel.CurrentProtein = _viewModel.CurrentFats = _viewModel.CurrentCarbs = _viewModel.CurrentWater = 0;

            foreach (var product in products)
            {
                _viewModel.SavedProducts.Add(product);
                _viewModel.CurrentCalories += product.Calories;
                _viewModel.CurrentProtein += product.Protein;
                _viewModel.CurrentFats += product.Fats;
                _viewModel.CurrentCarbs += product.Carbs;
                if ((product.Title ?? "").ToLower().Contains("вода"))
                    _viewModel.CurrentWater += product.Weight;
            }

            await Task.Delay(200);
            ForceUpdateCharts();
        }

        private void ForceUpdateCharts()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _viewModel.UpdatePieChart();
                UpdateCalorieDisplay();
            });
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NutritionModel product)
            {
                var result = MessageBox.Show($"Видалити '{product.Title}'?", "Підтвердження",
                                             MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;

                await DeleteProductFromFirestore(product);

                _viewModel.SavedProducts.Remove(product);
                _viewModel.CurrentCalories -= product.Calories;
                _viewModel.CurrentProtein -= product.Protein;
                _viewModel.CurrentFats -= product.Fats;
                _viewModel.CurrentCarbs -= product.Carbs;
                if ((product.Title ?? "").ToLower().Contains("вода"))
                    _viewModel.CurrentWater -= product.Weight;

                ForceUpdateCharts();

                var fs = new FirestoreService();
                await fs.RecomputeTodayAsync(UserSession.CurrentUserId);

            }
        }

        private async Task DeleteProductFromFirestore(NutritionModel product)
        {
            try
            {
                string userId = UserSession.CurrentUserId;
                if (string.IsNullOrEmpty(userId)) return;

                var db = new FirestoreService().GetFirestoreDb();
                var diaryRef = db.Collection("Users").Document(userId).Collection("FoodDiary");

                string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
                string timeStr = product.Time ?? "";

                var query = diaryRef
                    .WhereEqualTo("Date", dateStr)
                    .WhereEqualTo("Title", product.Title ?? "")
                    .WhereEqualTo("Weight", product.Weight)
                    .WhereEqualTo("Calories", product.Calories);

                if (!string.IsNullOrEmpty(timeStr))
                    query = query.WhereEqualTo("Time", timeStr);

                var snap = await query.GetSnapshotAsync();
                foreach (var doc in snap.Documents)
                    await doc.Reference.DeleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при видаленні запису FoodDiary: {ex.Message}");
            }
        }

        // Обработчик для TextBox: стирает плейсхолдер
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb &&
                (tb.Text == "Введіть калорії..." || tb.Text == "Введіть білки..." ||
                 tb.Text == "Введіть жири..." || tb.Text == "Введіть вуглеводи..."))
            {
                tb.Text = "";
                tb.Foreground = Brushes.Black;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrWhiteSpace(tb.Text))
            {
                if (tb.Name == "CaloriesInput") tb.Text = "Введіть калорії...";
                if (tb.Name == "ProteinInput") tb.Text = "Введіть білки...";
                if (tb.Name == "FatsInput") tb.Text = "Введіть жири...";
                if (tb.Name == "CarbsInput") tb.Text = "Введіть вуглеводи...";
                tb.Foreground = Brushes.Gray;
            }
        }
    }
}


