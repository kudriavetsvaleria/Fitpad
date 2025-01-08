using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Fitpad.View.Pages;
using Fitpad.ViewModel.PagesViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Fitpad.View.Pages
{
    public partial class AccountLoginPage : Page
    {
        private static AccountLoginPage _instance;
        private readonly UserRepository _userRepository;

        private AccountLoginPage(ProfileViewModel profileViewModel)
        {
            InitializeComponent();
            _userRepository = new UserRepository();
            DataContext = profileViewModel;
        }

        public static AccountLoginPage GetInstance(ProfileViewModel profileViewModel)
        {
            if (_instance == null)
            {
                _instance = new AccountLoginPage(profileViewModel);
            }
            return _instance;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Логин и пароль не могут быть пустыми.");
                return;
            }

            UserModel user = await _userRepository.GetUserAsync(username);
            if (user == null || !VerifyPassword(password, user.Password))
            {
                ShowError("Неверный логин или пароль.");
                return;
            }

            // Сохраняем ID текущего пользователя
            UserRepository.CurrentUserId = user.Id.ToString();

            var profileViewModel = new ProfileViewModel(user);
            MessageBox.Show("Авторизация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            NavigationService.Navigate(ProfilePage.GetInstance(profileViewModel));
        }

        private void NavigateToRegistrationPage_Click(object sender, RoutedEventArgs e)
        {
            // Логика перехода на страницу регистрации
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            // Логика для перехода к следующему шагу регистрации
        }

        private void PreviousStep_Click(object sender, RoutedEventArgs e)
        {
            // Логика для возврата к предыдущему шагу регистрации
        }

        private void TestStep_Click(object sender, RoutedEventArgs e)
        {
            // Логика для тестового перехода
        }

        private void SuccessOkButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика для обработки успешной регистрации
        }

        private void NavigateToLoginPage_Click(object sender, RoutedEventArgs e)
        {
            // Логика для перехода на страницу авторизации
        }


        private bool VerifyPassword(string enteredPassword, string storedPasswordHash)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] enteredBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(enteredPassword));
                string enteredHash = BitConverter.ToString(enteredBytes).Replace("-", "").ToLower();

                return enteredHash == storedPasswordHash;
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
