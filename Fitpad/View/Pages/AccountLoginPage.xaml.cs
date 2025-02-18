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

            LoadCurrentUserFromFile();
        }

        private void LoadCurrentUserFromFile()
        {
            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fitpad", "current_user.json");
                Console.WriteLine($"📂 Ищем файл: {filePath}");

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Console.WriteLine($"📜 JSON: {json}");

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("❌ Файл `current_user.json` пустой!");
                        return;
                    }

                    var data = JsonConvert.DeserializeObject<dynamic>(json);

                    if (data != null && data.UserId != null)
                    {
                        UserRepository.CurrentUserId = data.UserId.ToString();
                        Console.WriteLine($"✅ User ID загружен из файла: {UserRepository.CurrentUserId}");
                    }
                    else
                    {
                        Console.WriteLine("❌ Ошибка: данные в файле некорректны!");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Файл `current_user.json` не найден.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при загрузке UserID: {ex.Message}");
            }
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

                Console.WriteLine($"✅ Користувач {user.Name} успішно авторизований.");

                // 🔹 Сохраняем UserID в файл и устанавливаем в сессию
                UserSession.SaveUserIdToFile(user.Id);
                Console.WriteLine($"📌 Установлен UserID после авторизации: {UserSession.CurrentUserId}");

                // ✅ Обновляем MainViewModel, чтобы отображались все кнопки
                await MainViewModel.Instance.UpdateNavigationStateAsync();


                // ✅ Переход на страницу профиля
                NavigationService.Navigate(ProfilePage.GetInstance(new ProfileViewModel(user)));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка авторизации: {ex.Message}");
            }
        }



        // Метод для сохранения данных пользователя в JSON-файл
        private void SaveCurrentUserToFile(UserModel user)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "current_user.json");

                string json = JsonConvert.SerializeObject(user, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Console.WriteLine("✅ Данные пользователя сохранены.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения файла: {ex.Message}");
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
