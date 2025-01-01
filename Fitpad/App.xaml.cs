using System.Windows;
using System.Windows.Controls;
using Fitpad.Model;
using Fitpad.View.Pages;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var profileViewModel = new ProfileViewModel();
            var mainWindow = new MainWindow();

            if (mainWindow.Content is Frame frame)
            {
                if (profileViewModel.CurrentUser != null)
                {
                    frame.Navigate(new ProfilePage(profileViewModel));
                }
                else
                {
                    frame.Navigate(new AccountLoginPage(profileViewModel));
                }
            }
        }
    }
}
