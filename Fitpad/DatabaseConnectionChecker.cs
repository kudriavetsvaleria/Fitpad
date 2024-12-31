using System;
using System.Data.SQLite;
using System.Windows;

public static class DatabaseConnectionChecker
{
    public static void CheckDatabaseConnection(string connectionString)
    {
        try
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Попытка открыть соединение
                MessageBox.Show("Подключение к базе данных успешно установлено.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}");
        }
    }
}
