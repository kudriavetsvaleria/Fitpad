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
        private readonly ProfileViewModel _profileViewModel;

        public AccountLoginPage(ProfileViewModel profileViewModel)
        {
            InitializeComponent();
            _profileViewModel = profileViewModel;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Логин и пароль не могут быть пустыми.");
                return;
            }

            // Проверка в базе данных
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

            // Сохранение данных пользователя в ProfileViewModel
            _profileViewModel.SaveUserData(user);

            MessageBox.Show("Авторизация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
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
