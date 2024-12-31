using System.Windows;
using System.Windows.Controls;

namespace Fitpad.View.Pages
{
    public partial class AccountLoginPage : Page
    {
        public AccountLoginPage()
        {
            InitializeComponent();      
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // Пример простой проверки
            if (username == "admin" && password == "12345")
            {
                MessageBox.Show("Успешный вход!", "Авторизация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ErrorTextBlock.Text = "Неверный логин или пароль!";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }
    }
}
