using Fitpad.Model;
using Fitpad.Model.Entities;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Fitpad.View.Pages
{
    public partial class CaloriesPage : Page
    {
        public CaloriesPage()
        {
            InitializeComponent();
        }
        public CaloriesPage(int userId)
        {
            InitializeComponent();
            _currentUserId = userId; // Установка ID текущего пользователя
            ShowStep(1); // Показ первого шага
        }
        private int _currentStep = 1; // Текущий шаг
        private int _currentUserId; // ID текущего пользователя
        private UserInfoModel _userInfo = new UserInfoModel(); // Модель для хранения данных пользователя

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int nextStep = int.Parse(button.Tag.ToString());

            if (!ValidateCurrentStep(nextStep - 1))
            {
                return;
            }

            ShowStep(nextStep);
        }

        private void PreviousStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int previousStep = int.Parse(button.Tag.ToString());

            ShowStep(previousStep);
        }

        private void ShowStep(int step)
        {
            Step1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
            Step4.Visibility = step == 4 ? Visibility.Visible : Visibility.Collapsed;
            Step5.Visibility = step == 5 ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool ValidateCurrentStep(int step)
        {
            switch (step)
            {
                case 1:
                    if (GenderInput.SelectedItem == null)
                    {
                        ShowError("Выберите ваш пол.");
                        return false;
                    }
                    _userInfo.Gender = (GenderInput.SelectedItem as ComboBoxItem)?.Content.ToString();
                    break;
                case 2:
                    if (!int.TryParse(AgeInput.Text, out int age) || age <= 0)
                    {
                        ShowError("Введите корректный возраст.");
                        return false;
                    }
                    _userInfo.Age = age;
                    break;
                case 3:
                    if (!int.TryParse(HeightInput.Text, out int height) || height <= 50 || height >= 300)
                    {
                        ShowError("Введите корректный рост в сантиметрах (от 50 до 300).");
                        return false;
                    }
                    _userInfo.Height = height;
                    break;
                case 4:
                    if (!double.TryParse(WeightInput.Text, out double weight) || weight <= 0)
                    {
                        ShowError("Введите корректный вес.");
                        return false;
                    }
                    _userInfo.Weight = weight;
                    break;
                case 5:
                    if (ActivityLevelInput.SelectedItem == null)
                    {
                        ShowError("Выберите уровень активности.");
                        return false;
                    }
                    _userInfo.ActivityLevel = (ActivityLevelInput.SelectedItem as ComboBoxItem)?.Content.ToString();
                    break;
            }

            ErrorTextBlock.Visibility = Visibility.Collapsed;
            return true;
        }


        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = UserStorage.GetCurrentUser();
            if (currentUser == null)
            {
                MessageBox.Show("Пожалуйста, выполните авторизацию перед заполнением данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Явно извлекаем значение из ComboBox
            _userInfo.ActivityLevel = (ActivityLevelInput.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrWhiteSpace(_userInfo.ActivityLevel))
            {
                ShowError("Пожалуйста, выберите уровень активности.");
                return;
            }

            using (var context = new ApplicationDbContext())
            {
                _currentUserId = currentUser.Id;

                var existingUserInfo = context.UserInfos.FirstOrDefault(u => u.UserId == _currentUserId);

                if (existingUserInfo != null)
                {
                    existingUserInfo.Gender = _userInfo.Gender;
                    existingUserInfo.Age = _userInfo.Age;
                    existingUserInfo.Height = _userInfo.Height;
                    existingUserInfo.Weight = _userInfo.Weight;
                    existingUserInfo.ActivityLevel = _userInfo.ActivityLevel;
                }
                else
                {
                    _userInfo.UserId = _currentUserId;
                    context.UserInfos.Add(_userInfo);
                }

                MessageBox.Show($"ActivityLevel перед сохранением: {_userInfo.ActivityLevel}"); // Для отладки

                context.SaveChanges();
            }

            MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            NavigationService.GoBack();
        }

    }
}
