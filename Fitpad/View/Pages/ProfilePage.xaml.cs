using Fitpad.Services;
using Fitpad.ViewModel.PagesViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.IO;

namespace Fitpad.View.Pages
{
    public partial class ProfilePage : Page
    {
        private static ProfilePage _instance;
        private static readonly object _lock = new object();
        private readonly ProfileViewModel _profileViewModel;

        public ProfilePage(ProfileViewModel profileViewModel)
        {
            InitializeComponent();
            _profileViewModel = profileViewModel;
            DataContext = profileViewModel;

            if (_profileViewModel.CurrentUser != null)
            {
                Console.WriteLine($"🔹 Загружаем данные анкеты для пользователя: {_profileViewModel.CurrentUser.Id}");
                _ = LoadUserInfoAsync(_profileViewModel.CurrentUser.Id);
            }
            else
            {
                Console.WriteLine("❌ Нет текущего пользователя!");
            }
        }


        public static ProfilePage GetInstance(ProfileViewModel profileViewModel = null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new ProfilePage(profileViewModel ?? new ProfileViewModel());
                }
                else if (profileViewModel != null)
                {
                    return _instance;
                }

                // Загружаем данные анкеты для текущего пользователя
                if (_instance._profileViewModel.CurrentUser != null)
                {
                    _ = _instance.LoadUserInfoAsync(_instance._profileViewModel.CurrentUser.Id);
                }

                return _instance;
            }
        }

        public void UpdateProfileData(ProfileViewModel profileViewModel)
        {
            if (profileViewModel != null)
            {
                _profileViewModel.CurrentUser = profileViewModel.CurrentUser;
                _profileViewModel.CurrentUserInfo = profileViewModel.CurrentUserInfo;
                DataContext = _profileViewModel; // 🔹 Обновляем DataContext
                Console.WriteLine("🔄 Данные профиля обновлены!");
            }
        }


        // Асинхронный метод для загрузки данных анкеты
        private async Task LoadUserInfoAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                Console.WriteLine("❌ Ошибка: UserId пустой!");
                return;
            }

            Console.WriteLine($"🔹 Загружаем данные пользователя с ID: {userId}");

            var firestoreService = new FirestoreService();
            var userInfo = await firestoreService.GetUserInfoAsync(userId);

            if (userInfo != null)
            {
                Console.WriteLine("✅ Дані анкети успішно завантажені:");
                Console.WriteLine($"Стать: {userInfo.Gender}, Вік: {userInfo.Age}, Зріст: {userInfo.Height}, Вага: {userInfo.Weight}");

                _profileViewModel.CurrentUserInfo = userInfo;
            }
            else
            {
                Console.WriteLine("❌ Дані анкети не знайдено.");
            }
        }



        public static void ResetInstance()
        {
            _instance = null;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _profileViewModel.ClearUserData();
            ResetInstance();
            ClearCurrentUserFile();
            NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
        }

        private void ClearCurrentUserFile()
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current_user.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка під час видалення файлу даних користувача: {ex.Message}");
            }
        }
    }
}
