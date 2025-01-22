using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Fitpad.View.Pages;
using Fitpad.ViewModel.PagesViewModels;
using Newtonsoft.Json;
using System;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace Fitpad.View.Pages
{
    public partial class AccountLoginPage : Page
    {
        private static AccountLoginPage _instance;
        private static readonly object _lock = new object();
        private readonly UserRepository _userRepository;

        // Сделать конструктор публичным
        public AccountLoginPage(ProfileViewModel profileViewModel)
        {
            InitializeComponent();
            _userRepository = new UserRepository();
            DataContext = profileViewModel;
        }

        public static AccountLoginPage GetInstance(ProfileViewModel profileViewModel)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new AccountLoginPage(profileViewModel);
                }
                return _instance;
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Логін і пароль не можуть бути порожніми.");
                return;
            }

            try
            {
                UserModel user = await _userRepository.GetUserAsync(username);
                if (user == null)
                {
                    ShowError("Користувач не знайдений.");
                    return;
                }

                if (!VerifyPassword(password, user.Password))
                {
                    ShowError("Неправильний пароль.");
                    return;
                }

                Console.WriteLine($"Користувач {user.Name} успішно авторизований.");

                UserRepository.CurrentUserId = user.Id.ToString();
                var profileViewModel = new ProfileViewModel(user);

                // Сохраняем данные пользователя в файл
                SaveCurrentUserToFile(user);
                UserRepository.CurrentUserId = user.Id.ToString();

                MessageBox.Show("Авторизация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.Navigate(ProfilePage.GetInstance(profileViewModel));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка авторизации: {ex.Message}");
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для сохранения данных пользователя в JSON-файл
private void SaveCurrentUserToFile(UserModel user)
{
    try
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current_user.json");

        // Сохраняем ID текущего пользователя вместе с данными пользователя
        var data = new
        {
            UserId = user.Id,
            User = user
        };

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(filePath, json);

        UserRepository.CurrentUserId = user.Id; // Устанавливаем CurrentUserId
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при сохранении данных пользователя: {ex.Message}");
    }
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

        private void NavigateToRegistrationPage_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(AccountRegistrationPage.GetInstance());
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
