using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Fitpad.View.Pages;
using Fitpad.ViewModel.PagesViewModels;
using Newtonsoft.Json;
using System;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Fitpad.View.Components;
using NLog;

namespace Fitpad.View.Pages
{
    public partial class AccountLoginPage : Page
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static AccountLoginPage _instance;
        private static readonly object _lock = new object();
        private readonly UserRepository _userRepository;

        public AccountLoginPage(DashboardViewModel DashboardViewModel)
        {
            InitializeComponent();
            _userRepository = new UserRepository();
            DataContext = DashboardViewModel;

        }


      

    public static AccountLoginPage GetInstance(DashboardViewModel DashboardViewModel)
    {
        lock (_lock)
        {
            if (_instance == null)
            {
                _instance = new AccountLoginPage(DashboardViewModel);
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
                UserRepository userRepository = new UserRepository();
                UserModel user = await userRepository.GetUserAsync(username);

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

                Logger.Info($"Користувач {user.Name} успішно авторизований.");

       
                UserSession.SaveUserIdToFile(user.Id);

                UserInfoModel userInfo = await userRepository.GetUserInfoAsync(user.Id);

                bool isInfoMissing =
                    userInfo == null ||
                    userInfo.Age <= 0 ||
                    userInfo.Height <= 0 ||
                    userInfo.Weight <= 0 ||
                    string.IsNullOrWhiteSpace(userInfo.ActivityLevel) ||
                    string.IsNullOrWhiteSpace(userInfo.Purpose);

                if (isInfoMissing)
                {
                    Logger.Info("Дані користувача не заповнені. Відкриваємо форму UserInfo.");
                    var vm = new DashboardViewModel(user)
                    {
                        CurrentUserInfo = userInfo // може бути null — UserInfoWindow сам створить
                    };
                    var userInfoWindow = new UserInfoWindow(vm);
                    userInfoWindow.ShowDialog();
                }



                await MainViewModel.Instance.UpdateNavigationStateAsync();

                NavigationService.Navigate(DashboardPage.GetInstance(new DashboardViewModel(user)));

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Помилка авторизації");
                MessageBox.Show($"Помилка авторизації: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SaveCurrentUserToFile(UserModel user)
        {
            try
            {
                
                string projectPath = Directory.GetCurrentDirectory();
                string resourcesPath = Path.Combine(projectPath, "Resources");
                string filePath = Path.Combine(resourcesPath, "current_user.json");

                if (!Directory.Exists(resourcesPath))
                {
                    Directory.CreateDirectory(resourcesPath);
                }

      
                string json = JsonConvert.SerializeObject(user, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Logger.Debug($"Данные пользователя сохранены в {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка сохранения файла");
            }
        }



        /// <summary>
        /// Проверяет пароль используя BCrypt.Verify
        /// (безопасное сравнение хешей, защищено от timing attacks)
        /// </summary>
        private bool VerifyPassword(string enteredPassword, string storedPasswordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(enteredPassword, storedPasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Fallback для старых SHA256 хешей (для миграции)
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] enteredBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(enteredPassword));
                    string enteredHash = BitConverter.ToString(enteredBytes).Replace("-", "").ToLower();
                    return enteredHash == storedPasswordHash;
                }
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
