using System.Windows;
using System.Windows.Controls;
using Fitpad.View.Pages;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var authRepository = new Model.Repositories.AuthRepository();
            var currentUser = authRepository.LoadAuthState();

            var profileViewModel = new ProfileViewModel();
            var mainWindow = new MainWindow();

            if (mainWindow.Content is Frame frame)
            {
                if (currentUser != null)
                {
                    // Сохраняем текущего пользователя в профиле
                    profileViewModel.SaveUserData(currentUser);
                    frame.Navigate(new ProfilePage(profileViewModel)); // Передаем profileViewModel
                }
                else
                {
                    // Передаем экземпляр ProfileViewModel
                    frame.Navigate(new AccountLoginPage(profileViewModel));
                }
            }
        }
    }
}
