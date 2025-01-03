using System.Linq;
using System;
using System.Windows;
using System.Windows.Controls;
using Fitpad.Model;
using Fitpad.View.Pages;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad.View.Pages
{
    public partial class AccountLoginPage : Page
    {
        private static AccountLoginPage _instance;
        private readonly ProfileViewModel _profileViewModel;

        public AccountLoginPage(ProfileViewModel profileViewModel)
        {
            InitializeComponent();
            _profileViewModel = profileViewModel;
            DataContext = _profileViewModel;
        }

        public static AccountLoginPage GetInstance(ProfileViewModel profileViewModel)
        {
            if (_instance == null)
            {
                _instance = new AccountLoginPage(profileViewModel);
            }
            return _instance;
        }



        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Логин и пароль не могут быть пустыми.");
                return;
            }

            UserModel user;
            using (var context = new ApplicationDbContext())
            {
                user = context.Users.FirstOrDefault(u => u.Username == username);
                if (user == null || !VerifyPassword(password, user.Password))
                {
                    ShowError("Неверный логин или пароль.");
                    return;
                }
            }

            _profileViewModel.SaveUserData(user); // Сохраняем данные в файл

            MessageBox.Show("Авторизация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            NavigationService.Navigate(NewsPage.GetInstance());
        }


        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void NavigateToRegistrationPage_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(AccountRegistrationPage.GetInstance());
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
    }
}
