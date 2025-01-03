using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad.View.Pages
{
    public partial class ProfilePage : Page
    {
        public ProfilePage(ProfileViewModel profileViewModel)
        {
            InitializeComponent();
            DataContext = profileViewModel; // Устанавливаем DataContext
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем данные пользователя (если требуется)
            var profileViewModel = DataContext as ProfileViewModel;
            profileViewModel?.ClearUserData();

            // Переход на страницу авторизации
            NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
        }
    }
}
