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


namespace Fitpad.View.Pages
{
    public partial class CalculateNutritionPage : Page
    {
        private readonly CalculateNutritionViewModel _viewModel;
        private readonly TranslatorService _translatorService;
        private readonly FirestoreDb _firestoreDb;
        private static string _currentUserId = string.Empty;


        public CalculateNutritionPage() : this(new UserInfoModel()) { }

        public CalculateNutritionPage(UserInfoModel userInfo)
        {
            InitializeComponent();
            var firestoreService = new FirestoreService();
            _firestoreDb = firestoreService.GetFirestoreDb();

            // Проверяем пользователя
            CheckUserAndUpdateData();

            if (userInfo == null)
            {
                MessageBox.Show("Ошибка: данные пользователя отсутствуют!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _viewModel = new CalculateNutritionViewModel(userInfo);
            DataContext = _viewModel;
            Console.WriteLine("📊 DataContext установлен!");

            // Подписка на обновление данных
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.CalorieDisplayText))
                {
                    CalorieIntakeText.Text = _viewModel.CalorieDisplayText;
                }
            };

            // Устанавливаем изначальное значение нормы калорий
            double initialCalories = 0;
            double dailyCalories = _viewModel.CalculateDailyCalorieIntake(_viewModel.UserInfo);

            UpdateCalorieDisplay(initialCalories, dailyCalories);

        }

        private void UpdateCalorieDisplay(double addedCalories, double? dailyCalorieNorm = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (CalorieIntakeText != null)
                {
                    // Если норма не передана, вычисляем её
                    dailyCalorieNorm ??= _viewModel.UserInfo != null ? CalculateDailyCalorieIntake(_viewModel.UserInfo) : 0;

                    string[] calorieParts = CalorieIntakeText.Text.Split('/');
                    double currentCalories = double.TryParse(calorieParts[0].Trim(), out double parsedCurrent) ? parsedCurrent : 0;

                    double newCalories = currentCalories + addedCalories;
                    CalorieIntakeText.Text = $"Ккал: {newCalories:0.0} / {dailyCalorieNorm:0.0}";
                }
            });
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
            var userInfo = await GetUserInfoAsync(userId);
            if (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0)
            {
                MessageBox.Show("Ошибка: Данные пользователя некорректны!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ✅ Обновляем норму калорий
            _viewModel.CalorieNorm = _viewModel.CalculateDailyCalorieIntake(userInfo);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (CalorieIntakeText != null)
                {
                    string[] calorieParts = CalorieIntakeText.Text.Split('/');
                    double currentCalories = double.TryParse(calorieParts[0].Trim(), out double parsedCurrent) ? parsedCurrent : 0;
                    CalorieIntakeText.Text = $"Ккал: {currentCalories:0.0} / {_viewModel.CalorieNorm:0.0}";
                }
            });
        }

        private async Task<UserInfoModel> GetUserInfoAsync(string userId)
        {
            var userInfoDoc = await _firestoreDb.Collection("UserInfos").Document(userId).GetSnapshotAsync();
            return userInfoDoc.Exists ? userInfoDoc.ConvertTo<UserInfoModel>() : null;
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

            var product = await _viewModel.SearchAndAddProductAsync(productName, weight);

            if (product != null)
            {
                Console.WriteLine($"✅ Додано продукт: {product.Title}, калорії: {product.Calories}");
            }
            else
            {
                MessageBox.Show("Продукт не знайдено", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public double CalculateDailyCalorieIntake(UserInfoModel userInfo)
        {
            if (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0)
            {
                Console.WriteLine("⚠ Ошибка: Некорректные пользовательские данные!");
                return 2000; // ✅ Возвращаем базовое значение, если данные отсутствуют
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



    }
}

