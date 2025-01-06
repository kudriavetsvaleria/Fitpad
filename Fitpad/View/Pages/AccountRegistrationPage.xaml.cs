using Fitpad.Model;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Fitpad.ViewModel.PagesViewModels;
using System.Linq;

namespace Fitpad.View.Pages
{
    public partial class AccountRegistrationPage : Page
    {
        private static AccountRegistrationPage _instance;
        public AccountRegistrationPage()
        {
            InitializeComponent();
        }

        public static AccountRegistrationPage GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AccountRegistrationPage();
            }
            return _instance;
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

        private void PreviousStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int previousStep = int.Parse(button.Tag.ToString());

            ShowStep(previousStep);
        }
        private void TestStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int nextStep = int.Parse(button.Tag.ToString());

            // Показываем следующий шаг без валидации
            ShowStep(nextStep);

            // Дополнительно можно показать уведомление для теста
            ShowNotification("Тестовый переход выполнен успешно.");
        }

        private void ShowStep(int step)
        {
            Step1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;

            // Скрываем кнопку авторизации и надпись "Есть аккаунт?" на шагах 2 и 3
            bool isFirstStep = (step == 1);
            LoginButton.Visibility = isFirstStep ? Visibility.Visible : Visibility.Collapsed;
            AccountTextBlock.Visibility = isFirstStep ? Visibility.Visible : Visibility.Collapsed;
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
                    using (var context = new ApplicationDbContext())
                    {
                        if (context.Users.Any(u => u.Username == UsernameTextBox.Text))
                        {
                            ShowError("Логин уже занят. Пожалуйста, выберите другой.");
                            return false;
                        }
                    }
                    break;

                case 2:
                    if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                    {
                        ShowError("Почта не может быть пустой.");
                        return false;
                    }
                    if (!IsValidEmail(EmailTextBox.Text))
                    {
                        ShowError("Введите корректный адрес электронной почты.");
                        return false;
                    }
                    using (var context = new ApplicationDbContext())
                    {
                        if (context.Users.Any(u => u.Email == EmailTextBox.Text))
                        {
                            ShowError("Почта уже используется. Пожалуйста, используйте другую.");
                            return false;
                        }
                    }
                    break;

                case 3:
                    if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                    {
                        ShowError("Пароль не может быть пустым.");
                        return false;
                    }
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

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateCurrentStep(3))
            {
                return;
            }

            string username = UsernameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            string hashedPassword = HashPassword(password);

            using (var context = new ApplicationDbContext())
            {
                var user = new UserModel
                {
                    Username = username,
                    Email = email,
                    Password = hashedPassword
                };

                context.Users.Add(user);
                context.SaveChanges();
            }

            ShowSuccessMessage();
        }

        private void ShowSuccessMessage()
        {
            SuccessMessageOverlay.Visibility = Visibility.Visible;
        }

        private void SuccessOkButton_Click(object sender, RoutedEventArgs e)
        {
            SuccessMessageOverlay.Visibility = Visibility.Collapsed;
            NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
        }



        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void ShowNotification(string message)
        {
            NotificationTextBlock.Text = message;
            NotificationTextBlock.Visibility = Visibility.Visible;

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, args) =>
            {
                NotificationTextBlock.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        private void NavigateToLoginPage_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
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
