using Fitpad.Model;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Fitpad.View.Pages
{
    public partial class AccountRegistrationPage : Page
    {
        public AccountRegistrationPage()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;
            string heightText = HeightTextBox.Text;
            string weightText = WeightTextBox.Text;
            DateTime? birthDate = BirthDatePicker.SelectedDate;

            // Преобразование данных без проверки
            int.TryParse(heightText, out int height); // Если некорректно, height будет 0
            double.TryParse(weightText, out double weight); // Если некорректно, weight будет 0

            using (var context = new ApplicationDbContext())
            {
                var user = new UserModel
                {
                    Username = username,
                    Email = email,
                    Password = password,
                    Height = height,
                    Weight = weight,
                    BirthDate = birthDate ?? DateTime.Now // Если дата не указана, используется текущая
                };

                context.Users.Add(user);
                context.SaveChanges();
            }


            MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
