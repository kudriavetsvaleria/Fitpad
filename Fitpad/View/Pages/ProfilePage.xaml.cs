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

        private ProfilePage(ProfileViewModel profileViewModel)
        {
            InitializeComponent();
            _profileViewModel = profileViewModel;
            DataContext = profileViewModel;
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
                    _instance._profileViewModel.UpdateUserData(profileViewModel.CurrentUser);
                }

                // Загружаем данные анкеты для текущего пользователя
                if (_instance._profileViewModel.CurrentUser != null)
                {
                    _ = _instance.LoadUserInfoAsync(_instance._profileViewModel.CurrentUser.Id);
                }

                return _instance;
            }
        }

        // Асинхронный метод для загрузки данных анкеты
        private async Task LoadUserInfoAsync(string userId)
        {
            var firestoreService = new FirestoreService();
            var userInfo = await firestoreService.GetUserInfoAsync(userId);

            if (userInfo != null)
            {
                Console.WriteLine("Данные анкеты успешно загружены.");
                _profileViewModel.CurrentUserInfo = userInfo;
            }
            else
            {
                Console.WriteLine("Данные анкеты не найдены.");
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

            // Удаляем файл с данными пользователя
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
                Console.WriteLine($"Ошибка при удалении файла данных пользователя: {ex.Message}");
            }
        }
    }
}
