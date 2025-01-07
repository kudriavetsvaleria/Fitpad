using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad.View.Pages
{
    public partial class ProfilePage : Page
    {
        private static ProfilePage _instance;
        private readonly ProfileViewModel _profileViewModel;
        private static readonly object _lock = new object(); // Для потокобезопасности

        public ProfilePage(ProfileViewModel profileViewModel)
        {
            InitializeComponent();
            _profileViewModel = profileViewModel;
            DataContext = profileViewModel; // Устанавливаем DataContext
        }

        // Метод для получения единственного экземпляра страницы
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
                    // Обновляем данные, если передан новый экземпляр ViewModel
                    _instance._profileViewModel.UpdateUserData(profileViewModel.CurrentUser);
                }
                return _instance;
            }
        }
        // Метод для очистки экземпляра при выходе из аккаунта
        public static void ResetInstance()
        {
            _instance = null;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _profileViewModel.ClearUserData();
            ResetInstance();
            NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
        }

    }
}
