using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Fitpad.ViewModel.PagesViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Threading.Tasks;
using Fitpad.Services;

namespace Fitpad.View.Pages
{
    public partial class AccountRegistrationPage : Page
    {
        private static AccountRegistrationPage _instance;
        private readonly UserRepository _userRepository;

        public AccountRegistrationPage()
        {
            InitializeComponent();
            _userRepository = new UserRepository(); // Инициализируем репозиторий
        }

        public static AccountRegistrationPage GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AccountRegistrationPage();
            }
            return _instance;
        }

        // Обработчик для кнопки "Далее" на первом шаге
        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (int.TryParse(button.Tag.ToString(), out int nextStep))
            {
                ShowStep(nextStep);
            }
            else
            {
                MessageBox.Show("Некорректное значение шага.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ShowStep(nextStep);
        }

        // Обработчик для кнопки "Назад" на шагах 2 и 3
        private void PreviousStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int previousStep = int.Parse(button.Tag.ToString());
            ShowStep(previousStep);
        }


        // Обработчик для кнопки "Регистрация"
        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем данные из полей ввода
                string name = UsernameTextBox.Text.Trim();
                string email = EmailTextBox.Text.Trim();
                string password = PasswordBox.Password.Trim();
                string confirmPassword = ConfirmPasswordBox.Password.Trim();

                // Проверяем, что все поля заполнены
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем совпадение паролей
                if (password != confirmPassword)
                {
                    MessageBox.Show("Пароли не совпадают. Попробуйте еще раз.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем новый объект пользователя
                var newUser = new UserModel
                {
                    Id = Guid.NewGuid().ToString(), // Генерируем уникальный идентификатор пользователя
                    Name = name,
                    Email = email,
                    Password = HashPassword(password) // Хэшируем пароль
                };

                // Используем RegistrationService для сохранения пользователя в Firebase
                var registrationService = new RegistrationService();
                await registrationService.RegisterUserAsync(newUser);

                // Показ успешного сообщения о регистрации
                SuccessMessageOverlay.Visibility = Visibility.Visible;
                MessageBox.Show("Регистрация завершена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // После успешной регистрации переходим на страницу авторизации
                NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] passwordBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(passwordBytes).Replace("-", "").ToLower();
            }
        }



        // Обработчик для кнопки "Авторизация"
        private void NavigateToLoginPage_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
        }

        // Обработчик для кнопки "Тест"
        private void TestStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (int.TryParse(button.Tag.ToString(), out int nextStep))
            {
                ShowStep(nextStep);
            }
            else
            {
                MessageBox.Show("Ошибка: Неверный формат номера шага.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ShowStep(nextStep);
        }




        // Обработчик для кнопки "ОК" после успешной регистрации
        private void SuccessOkButton_Click(object sender, RoutedEventArgs e)
        {
            SuccessMessageOverlay.Visibility = Visibility.Collapsed;
            NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
        }

        // Переход между шагами регистрации
        private void ShowStep(int step)
        {
            Step1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
