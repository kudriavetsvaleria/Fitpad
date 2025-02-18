using System;
using System.Windows;
using System.Windows.Controls;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using System.Threading.Tasks;
using Fitpad.View.Pages;
using System.IO;

namespace Fitpad.View.Components
{
    public partial class UserInfoForm : UserControl
    {
        private readonly UserInfoRepository _userInfoRepository;
        private readonly string _userId;

        public UserInfoForm()
        {
            InitializeComponent();
            _userInfoRepository = new UserInfoRepository();
        }

        public UserInfoForm(UserModel user) : this()
        {
            if (user == null || string.IsNullOrEmpty(user.Id))
            {
                MessageBox.Show("Помилка: користувач не знайдений!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _userId = user.Id;
            Console.WriteLine($"Инициализация формы UserInfoForm для пользователя с ID: {_userId}");
        }

        public static void Logout()
        {
            // ✅ Используем `UserSession.CurrentUserId` вместо `CurrentUserId`
            UserSession.CurrentUserId = string.Empty;

            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fitpad", "current_user.json");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            Console.WriteLine("🔹 UserSession очищен. Пользователь вышел.");
        }


        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ Валидация полей
                if (!ValidateInputs(out string gender, out int age, out int height, out double weight, out string activityLevel, out string purpose))
                {
                    return;
                }

                if (string.IsNullOrEmpty(_userId))
                {
                    MessageBox.Show("Помилка: ID користувача відсутній!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var userInfo = new UserInfoModel
                {
                    UserId = _userId,
                    Gender = gender,
                    Age = age,
                    Height = height,
                    Weight = weight,
                    ActivityLevel = activityLevel,
                    Purpose = purpose
                };

                // ✅ Сохранение данных с обработкой возможных ошибок
                await SaveUserInfoAsync(userInfo);

                // ✅ Обновление UI после успешного сохранения
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainViewModel = MainViewModel.Instance;
                    if (mainViewModel != null)
                    {
                        Console.WriteLine("✅ Данные сохранены, открываем калькулятор!");
                        mainViewModel.CurrentPage = new CalculateNutritionPage();
                    }
                });

                MessageBox.Show("Дані успішно збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка під час збереження даних: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs(out string gender, out int age, out int height, out double weight, out string activityLevel, out string purpose)
        {
            // 🔹 Устанавливаем начальные значения для `out` параметров
            gender = "";
            age = 0;
            height = 0;
            weight = 0;
            activityLevel = "";
            purpose = "";

            gender = (GenderInput?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string ageText = AgeInput?.Text;
            string heightText = HeightInput?.Text;
            string weightText = WeightInput?.Text;
            activityLevel = (ActivityLevelInput?.SelectedItem as ComboBoxItem)?.Content?.ToString();


            if (string.IsNullOrWhiteSpace(gender) || string.IsNullOrWhiteSpace(activityLevel) || string.IsNullOrWhiteSpace(purpose))
            {
                MessageBox.Show("Будь ласка, виберіть усі необхідні параметри.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(ageText, out age) || age <= 0 || age > 120)
            {
                MessageBox.Show("Помилка: введіть коректний вік (від 1 до 120 років).", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(heightText, out height) || height < 50 || height > 250)
            {
                MessageBox.Show("Помилка: введіть коректний зріст (50 - 250 см).", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!double.TryParse(weightText, out weight) || weight < 10 || weight > 300)
            {
                MessageBox.Show("Помилка: введіть коректну вагу (10 - 300 кг).", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }


        private async Task SaveUserInfoAsync(UserInfoModel userInfo)
        {
            try
            {
                await _userInfoRepository.SaveUserInfoAsync(userInfo);
                Console.WriteLine("✅ Дані користувача успішно збережені!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Помилка збереження даних: {ex.Message}");
                throw;
            }
        }
    }
}
