using Fitpad.Model;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fitpad.View.Pages
{
    public partial class AccountRegistrationPage : Page
    {
        public AccountRegistrationPage()
        {
            InitializeComponent();
            BirthDateTextBox.PreviewTextInput += BirthDateTextBox_PreviewTextInput;
            BirthDateTextBox.TextChanged += BirthDateTextBox_TextChanged;
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int nextStep = int.Parse(button.Tag.ToString());

            if (!ValidateCurrentStep(nextStep - 1))
            {
                return;
            }

            ShowStep(nextStep);
        }

        private void TestStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int nextStep = int.Parse(button.Tag.ToString());
            ShowStep(nextStep);
        }

        private void PreviousStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int previousStep = int.Parse(button.Tag.ToString());

            ShowStep(previousStep);
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

        private bool ValidateCurrentStep(int step)
        {
            switch (step)
            {
                case 1:
                    if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
                    {
                        ShowError("Логин не может быть пустым.");
                        return false;
                    }
                    break;
                case 2:
                    if (!IsValidEmail(EmailTextBox.Text))
                    {
                        ShowError("Введите корректный адрес электронной почты.");
                        return false;
                    }
                    break;
                case 3:
                    if (PasswordBox.Password.Length < 8)
                    {
                        ShowError("Пароль должен содержать не менее 8 символов.");
                        return false;
                    }
                    if (PasswordBox.Password != ConfirmPasswordBox.Password)
                    {
                        ShowError("Пароли не совпадают.");
                        return false;
                    }
                    break;
                case 4:
                    if (!int.TryParse(HeightTextBox.Text, out int height) || height < 50 || height > 300)
                    {
                        ShowError("Введите корректный рост (от 50 до 300 см).");
                        return false;
                    }
                    break;
                case 5:
                    if (!double.TryParse(WeightTextBox.Text, out double weight) || weight < 10 || weight > 200)
                    {
                        ShowError("Введите корректный вес (от 10 до 200 кг).");
                        return false;
                    }
                    break;
                case 6:
                    if (!IsValidBirthDate(BirthDateTextBox.Text))
                    {
                        ShowError($"Введите корректную дату рождения в формате ДД.ММ.ГГГГ (1940 - {DateTime.Now.Year - 8}).");
                        return false;
                    }
                    break;
                default:
                    return true;
            }
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var mailAddress = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private bool IsValidBirthDate(string date)
        {
            if (!DateTime.TryParseExact(date, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                return false;
            }

            int year = parsedDate.Year;
            int currentYearMinus8 = DateTime.Now.Year - 8;

            return year >= 1940 && year <= currentYearMinus8;
        }

        private void BirthDateTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только ввод цифр и точек
            e.Handled = !Regex.IsMatch(e.Text, "[0-9.]");
        }

        private void BirthDateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string text = textBox.Text;

            if (text.Length == 2 || text.Length == 5)
            {
                if (!text.EndsWith("."))
                {
                    textBox.Text = text + ".";
                    textBox.CaretIndex = textBox.Text.Length; // Установить курсор в конец
                }
            }

            if (text.Length > 10)
            {
                textBox.Text = text.Substring(0, 10); // Ограничиваем длину
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateCurrentStep(6))
            {
                return;
            }

            string username = UsernameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;
            string heightText = HeightTextBox.Text;
            string weightText = WeightTextBox.Text;

            if (!DateTime.TryParseExact(BirthDateTextBox.Text, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime birthDate))
            {
                ShowError("Введите корректную дату рождения.");
                return;
            }

            string hashedPassword = HashPassword(password);

            using (var context = new ApplicationDbContext())
            {
                var user = new UserModel
                {
                    Username = username,
                    Email = email,
                    Password = hashedPassword,
                    Height = int.Parse(heightText),
                    Weight = double.Parse(weightText),
                    BirthDate = birthDate
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
