using Fitpad.Model;
using System;
using System.Security.Cryptography;
using System.Text;
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

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            // Скрыть текущий этап и показать следующий
            var button = sender as Button;
            int nextStep = int.Parse(button.Tag.ToString());

            ShowStep(nextStep);
        }

        private void ShowStep(int step)
        {
            Step1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
            Step4.Visibility = step == 4 ? Visibility.Visible : Visibility.Collapsed;
            Step5.Visibility = step == 5 ? Visibility.Visible : Visibility.Collapsed;
            Step6.Visibility = step == 6 ? Visibility.Visible : Visibility.Collapsed;
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

            // Проверка на совпадение паролей
            if (password != confirmPassword)
            {
                ShowError("Пароли не совпадают.");
                return;
            }

            // Преобразование данных без проверки
            int.TryParse(heightText, out int height); // Если некорректно, height будет 0
            double.TryParse(weightText, out double weight); // Если некорректно, weight будет 0

            // Хеширование пароля
            string hashedPassword = HashPassword(password);

            using (var context = new ApplicationDbContext())
            {
                var user = new UserModel
                {
                    Username = username,
                    Email = email,
                    Password = hashedPassword, // Сохраняем хеш вместо пароля
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

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}