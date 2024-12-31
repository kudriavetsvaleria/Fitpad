using System;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using BCrypt.Net;

namespace Fitpad.View.Pages
{
    public partial class AccountRegistrationPage : Page
    {
        private readonly string _connectionString = "Data Source=FitpadDB.db;Version=3;";

        public AccountRegistrationPage()
        {
            InitializeComponent();
            InitializeDatabase();
            GetAllUsers();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    @"CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        Email TEXT NOT NULL UNIQUE,
                        Password TEXT NOT NULL,
                        Height INTEGER NOT NULL,
                        Weight REAL NOT NULL,
                        BirthDate TEXT NOT NULL
                    );", connection);
                command.ExecuteNonQuery();
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;
            string heightText = HeightTextBox.Text.Trim();
            string weightText = WeightTextBox.Text.Trim();
            DateTime? birthdateNullable = BirthDatePicker.SelectedDate;

            // Валидация данных
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Логин не может быть пустым.");
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Поле email не может быть пустым.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password) || password != confirmPassword)
            {
                ShowError("Пароли не совпадают или пусты.");
                return;
            }

            if (!int.TryParse(heightText, out int height) || height <= 0)
            {
                ShowError("Введите корректный рост (в сантиметрах).");
                return;
            }

            if (!double.TryParse(weightText, out double weight) || weight <= 0)
            {
                ShowError("Введите корректный вес (в килограммах).");
                return;
            }

            if (birthdateNullable == null)
            {
                ShowError("Выберите дату рождения.");
                return;
            }

            DateTime birthdate = birthdateNullable.Value;

            if (IsUsernameTaken(username))
            {
                ShowError("Этот логин уже используется.");
                return;
            }

            if (IsEmailTaken(email))
            {
                ShowError("Этот адрес электронной почты уже зарегистрирован.");
                return;
            }

            string hashedPassword = HashPassword(password);

            try
            {
                AddUser(username, email, hashedPassword, height, weight, birthdate);
                MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearFields();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        private bool IsUsernameTaken(string username)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    @"SELECT COUNT(*) FROM Users WHERE Username = @Username;", connection);
                command.Parameters.AddWithValue("@Username", username);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private bool IsEmailTaken(string email)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    @"SELECT COUNT(*) FROM Users WHERE Email = @Email;", connection);
                command.Parameters.AddWithValue("@Email", email);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private void GetAllUsers()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT * FROM Users;", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string userInfo = $"ID: {reader["Id"]}, Username: {reader["Username"]}, Email: {reader["Email"]}, " +
                                          $"Height: {reader["Height"]}, Weight: {reader["Weight"]}, BirthDate: {reader["BirthDate"]}";
                        MessageBox.Show(userInfo); // Вывод в консоль для отладки
                    }
                }
            }
        }


        private void AddUser(string username, string email, string password, int height, double weight, DateTime birthdate)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    @"INSERT INTO Users (Username, Email, Password, Height, Weight, BirthDate) 
                      VALUES (@Username, @Email, @Password, @Height, @Weight, @BirthDate);", connection);

                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@Password", password);
                command.Parameters.AddWithValue("@Height", height);
                command.Parameters.AddWithValue("@Weight", weight);
                command.Parameters.AddWithValue("@BirthDate", birthdate.ToString("yyyy-MM-dd"));

                command.ExecuteNonQuery();
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private void ClearFields()
        {
            UsernameTextBox.Text = "";
            EmailTextBox.Text = "";
            PasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";
            HeightTextBox.Text = "";
            WeightTextBox.Text = "";
            BirthDatePicker.SelectedDate = null;
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}
