using Fitpad.ViewModel.PagesViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

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
                return _instance;
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

            NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
        }
    }
}
