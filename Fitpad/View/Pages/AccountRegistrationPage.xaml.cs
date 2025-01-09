using Fitpad.Model.Entities;
using Fitpad.Services;
using Fitpad.ViewModel.PagesViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Fitpad.View.Pages
{
    public partial class AccountRegistrationPage : Page
    {
        private static AccountRegistrationPage _instance;
        private static readonly object _lock = new object(); // Для потокобезопасности
        private readonly RegistrationService _registrationService;

        // Публичный конструктор без параметров, необходимый для Activator.CreateInstance
        public AccountRegistrationPage()
        {
            InitializeComponent();
            _registrationService = new RegistrationService();
        }

        public static AccountRegistrationPage GetInstance()
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new AccountRegistrationPage();
                }
                return _instance;
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string name = UsernameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();
            string confirmPassword = ConfirmPasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают. Попробуйте еще раз.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            { 
                var newUser = new UserModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Email = email,   
                    Password = HashPassword(password)
                };

                await _registrationService.RegisterUserAsync(newUser);

                MessageBox.Show("Регистрация завершена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        
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

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Tag.ToString(), out int nextStep))
            {
                ShowStep(nextStep);
            }
        }

        private void PreviousStep_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Tag.ToString(), out int previousStep))
            {
                ShowStep(previousStep);
            }
        }

        private void SuccessOkButton_Click(object sender, RoutedEventArgs e)
        {
            SuccessMessageOverlay.Visibility = Visibility.Collapsed;
            NavigationService.Navigate(new AccountLoginPage(new ProfileViewModel()));
        }

        private void ShowStep(int step)
        {
            Step1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NavigateToLoginPage_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AccountLoginPage(new ProfileViewModel()));
        }
    }
}
