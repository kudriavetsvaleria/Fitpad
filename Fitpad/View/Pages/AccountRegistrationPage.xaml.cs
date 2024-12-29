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
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;
            string heightText = HeightTextBox.Text;
            string weightText = WeightTextBox.Text;
            DateTime? birthDate = BirthDatePicker.SelectedDate;

            // Проверка логина
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Логин не может быть пустым.");
                return;
            }

            // Проверка пароля
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Пароль не может быть пустым.");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Пароли не совпадают.");
                return;
            }

            // Проверка роста
            if (!int.TryParse(heightText, out int height) || height <= 0)
            {
                ShowError("Введите корректный рост (в сантиметрах).");
                return;
            }

            // Проверка веса
            if (!double.TryParse(weightText, out double weight) || weight <= 0)
            {
                ShowError("Введите корректный вес (в килограммах).");
                return;
            }

            // Проверка даты рождения
            if (birthDate == null)
            {
                ShowError("Выберите дату рождения.");
                return;
            }

            // Успешная регистрация
            MessageBox.Show($"Регистрация успешна!\nЛогин: {username}\nРост: {height} см\nВес: {weight} кг\nДата рождения: {birthDate.Value.ToShortDateString()}",
                            "Регистрация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}
