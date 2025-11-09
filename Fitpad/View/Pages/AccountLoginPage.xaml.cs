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

namespace Fitpad.View.Pages
{
    public partial class AccountLoginPage : Page
    {
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

                Console.WriteLine($"Користувач {user.Name} успішно авторизований.");

       
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
                    Console.WriteLine("Дані користувача не заповнені. Відкриваємо форму UserInfo.");
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
                Console.WriteLine($"Помилка авторизації: {ex.Message}");
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

                Console.WriteLine($"Данные пользователя сохранены в {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения файла: {ex.Message}");
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
