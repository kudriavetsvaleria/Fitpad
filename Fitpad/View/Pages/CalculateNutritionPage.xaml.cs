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

        private string _manualEntryProductName;
        private double _manualEntryWeight;


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
                userInfo = new UserInfoModel(); // ✅ Создаем пустой объект, чтобы избежать ошибки
            }

            _viewModel = new CalculateNutritionViewModel(userInfo);
            DataContext = _viewModel;
            _viewModel.ShowManualEntryOverlayAction = ShowManualEntryOverlay;

            Console.WriteLine("📊 DataContext установлен!");
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

            // Проверяем, заполнены ли основные данные пользователя
            if (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0)
            {
                Console.WriteLine("❌ Данные пользователя отсутствуют или некорректны. Открываем форму UserInfoForm...");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainViewModel = MainViewModel.Instance;
                    if (mainViewModel != null)
                    {
                        Console.WriteLine("🔹 Открываем UserInfoForm в основном окне...");
                        mainViewModel.CurrentPage = new UserInfoForm();
                    }
                });

                // Ожидаем, пока данные будут заполнены
                while (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0)
                {
                    await Task.Delay(500); // Ожидание обновления данных
                    userInfo = await GetUserInfoAsync(userId);
                }

                Console.WriteLine("✅ Данные успешно заполнены. Открываем калькулятор...");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainViewModel = MainViewModel.Instance;
                    if (mainViewModel != null)
                    {
                        mainViewModel.CurrentPage = new CalculateNutritionPage(userInfo);
                    }
                });

                return; // Выходим из метода после загрузки калькулятора
            }

            // ✅ Обновляем норму калорий после получения данных пользователя
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

            Console.WriteLine("✅ Данные пользователя обновлены, калькулятор готов к работе.");
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

        }

        private void OnManualEntryConfirm(object sender, RoutedEventArgs e)
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

            _viewModel.SavedProducts.Add(manualProduct);
            _viewModel.CurrentCalories += manualProduct.Calories;
            _viewModel.CurrentProtein += manualProduct.Protein;
            _viewModel.CurrentFats += manualProduct.Fats;
            _viewModel.CurrentCarbs += manualProduct.Carbs;

            Console.WriteLine($"✅ Вручную добавлен продукт: {manualProduct.Title}, калорії: {manualProduct.Calories}");

            _viewModel.UpdatePieChart();
            HideManualEntryOverlay();
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

