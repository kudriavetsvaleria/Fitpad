using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Fitpad.Services;
using Fitpad.ViewModel.PagesViewModels;
using Google.Cloud.Firestore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Fitpad.View.Pages
{
    public partial class AccountRegistrationPage : Page
    {
        private static AccountRegistrationPage _instance;
        private static readonly object _lock = new object();

        private readonly RegistrationService _registrationService;
        private readonly UserRepository _userRepository = new UserRepository();
        private readonly FirestoreDb _db; // для перевірки унікальності

        public AccountRegistrationPage()
        {
            InitializeComponent();
            _registrationService = new RegistrationService();
            _db = FirestoreDbProvider.Instance.GetDb();

            ClearAllErrors();

            // live-валідація
            UsernameTextBox.TextChanged += UsernameTextBox_TextChanged;
            EmailTextBox.TextChanged += EmailTextBox_TextChanged;
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            ConfirmPasswordBox.PasswordChanged += ConfirmPasswordBox_PasswordChanged;
        }

        public static AccountRegistrationPage GetInstance()
        {
            lock (_lock)
            {
                if (_instance == null) _instance = new AccountRegistrationPage();
                return _instance;
            }
        }

        // ========================= ВАЛІДАЦІЯ =========================

        // логін: 3–20, букви (укр/лат), цифри, ., _, - ; без пробілів
        private static readonly Regex UsernameRx =
            new Regex(@"^[A-Za-zА-Яа-яІіЇїЄє0-9._-]{3,20}$", RegexOptions.Compiled);

        // простий e-mail
        private static readonly Regex EmailRx =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private static bool HasUpper(string s) => s.Any(char.IsUpper);
        private static bool HasLower(string s) => s.Any(char.IsLower);
        private static bool HasDigit(string s) => s.Any(char.IsDigit);
        private static bool NoSpaces(string s) => !s.Any(char.IsWhiteSpace);

        private string ValidateUsername(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Логін обов’язковий.";
            if (!UsernameRx.IsMatch(name))
                return "3–20 символів: букви/цифри/._- без пробілів.";
            return "";
        }

        private string ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "Пошта обов’язкова.";
            if (!EmailRx.IsMatch(email)) return "Некоректна адреса e-mail.";
            return "";
        }

        private string ValidatePassword(string pwd)
        {
            if (string.IsNullOrEmpty(pwd)) return "Пароль обов’язковий.";
            if (pwd.Length < 8) return "Мінімум 8 символів.";
            if (!HasUpper(pwd)) return "Має містити велику літеру.";
            if (!HasLower(pwd)) return "Має містити малу літеру.";
            if (!HasDigit(pwd)) return "Має містити цифру.";
            if (!NoSpaces(pwd)) return "Без пробілів.";
            return "";
        }

        private string ValidateConfirm(string pwd, string confirm)
        {
            if (string.IsNullOrEmpty(confirm)) return "Підтвердження пароля обов’язкове.";
            if (pwd != confirm) return "Паролі не збігаються.";
            return "";
        }

        // live-хендлери
        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => UsernameErrorText.Text = ValidateUsername(UsernameTextBox.Text);

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => EmailErrorText.Text = ValidateEmail(EmailTextBox.Text);

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordErrorText.Text = ValidatePassword(PasswordBox.Password);
            ConfirmPasswordErrorText.Text = ValidateConfirm(PasswordBox.Password, ConfirmPasswordBox.Password);
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
            => ConfirmPasswordErrorText.Text = ValidateConfirm(PasswordBox.Password, ConfirmPasswordBox.Password);

        private void ClearAllErrors()
        {
            UsernameErrorText.Text = "";
            EmailErrorText.Text = "";
            PasswordErrorText.Text = "";
            ConfirmPasswordErrorText.Text = "";
        }

        // ========================= КРОКИ ФОРМИ =========================

        private async void NextStep_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Tag?.ToString(), out int nextStep))
            {
                if (nextStep == 2)
                {
                    UsernameErrorText.Text = ValidateUsername(UsernameTextBox.Text);
                    if (!string.IsNullOrEmpty(UsernameErrorText.Text)) return;

                    // унікальність логіна
                    if (await IsUsernameTakenAsync(UsernameTextBox.Text.Trim()))
                    {
                        UsernameErrorText.Text = "Такий логін вже зайнятий.";
                        return;
                    }
                }
                else if (nextStep == 3)
                {
                    EmailErrorText.Text = ValidateEmail(EmailTextBox.Text);
                    if (!string.IsNullOrEmpty(EmailErrorText.Text)) return;

                    // унікальність пошти
                    if (await IsEmailTakenAsync(EmailTextBox.Text.Trim()))
                    {
                        EmailErrorText.Text = "Ця пошта вже використовується.";
                        return;
                    }
                }

                ShowStep(nextStep);
            }
        }

        private void PreviousStep_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Tag?.ToString(), out int prevStep))
                ShowStep(prevStep);
        }

        private void ShowStep(int step)
        {
            Step1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ========================= РЕЄСТРАЦІЯ =========================

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            UsernameErrorText.Text = ValidateUsername(UsernameTextBox.Text);
            EmailErrorText.Text = ValidateEmail(EmailTextBox.Text);
            PasswordErrorText.Text = ValidatePassword(PasswordBox.Password);
            ConfirmPasswordErrorText.Text = ValidateConfirm(PasswordBox.Password, ConfirmPasswordBox.Password);

            if (string.IsNullOrEmpty(UsernameErrorText.Text) &&
                await IsUsernameTakenAsync(UsernameTextBox.Text.Trim()))
                UsernameErrorText.Text = "Такий логін вже зайнятий.";

            if (string.IsNullOrEmpty(EmailErrorText.Text) &&
                await IsEmailTakenAsync(EmailTextBox.Text.Trim()))
                EmailErrorText.Text = "Ця пошта вже використовується.";

            if (!string.IsNullOrEmpty(UsernameErrorText.Text) ||
                !string.IsNullOrEmpty(EmailErrorText.Text) ||
                !string.IsNullOrEmpty(PasswordErrorText.Text) ||
                !string.IsNullOrEmpty(ConfirmPasswordErrorText.Text))
                return;

            var newUser = new UserModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = UsernameTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                Password = HashPassword(PasswordBox.Password.Trim())
            };

            try
            {
                await _registrationService.RegisterUserAsync(newUser);
                await _userRepository.SaveUserAsync(newUser);
                SuccessMessageOverlay.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ConfirmPasswordErrorText.Text = $"Помилка реєстрації: {ex.Message}";
            }
        }

        // ========================= ХЕЛПЕРИ =========================

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        // Перевірка унікальності логіна в Firestore
        private async System.Threading.Tasks.Task<bool> IsUsernameTakenAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            var snap = await _db.Collection("Users")
                                .WhereEqualTo("Name", username)
                                .Limit(1)
                                .GetSnapshotAsync();
            return snap.Count > 0;
        }

        // Перевірка унікальності e-mail в Firestore
        private async System.Threading.Tasks.Task<bool> IsEmailTakenAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var snap = await _db.Collection("Users")
                                .WhereEqualTo("Email", email)
                                .Limit(1)
                                .GetSnapshotAsync();
            return snap.Count > 0;
        }

        // ========================= ІНШЕ =========================

        private void StartConfetti(object sender, RoutedEventArgs e)
        {
            if (FindResource("ConfettiStoryboard") is Storyboard sb) sb.Begin();
        }

        private void CloseWelcomeOverlay(object sender, RoutedEventArgs e)
            => WelcomeOverlay.Visibility = Visibility.Collapsed;

        private void SuccessOkButton_Click(object sender, RoutedEventArgs e)
        {
            SuccessMessageOverlay.Visibility = Visibility.Collapsed;
            NavigationService.Navigate(new AccountLoginPage(new DashboardViewModel()));
        }

        private void NavigateToLoginPage_Click(object sender, RoutedEventArgs e)
            => NavigationService.Navigate(new AccountLoginPage(new DashboardViewModel()));
    }
}
