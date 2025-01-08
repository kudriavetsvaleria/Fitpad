using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks;
using Fitpad.Model.Entities;
using System.Windows;

namespace Fitpad.Model.Repositories
{
    public class UserRepository
    {
        private readonly FirestoreDb _firestoreDb;
        private const string UsersCollection = "Users";

        // Статическое поле для хранения ID текущего пользователя
        public static string CurrentUserId { get; set; }

        public UserRepository()
        {
            try
            {
                _firestoreDb = FirestoreDb.Create("fitpad-2025"); // Укажите ваш идентификатор проекта Firebase
                Console.WriteLine("Подключение к Firestore успешно установлено.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при подключении к Firestore: {ex.Message}");
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // Добавляем метод для получения пользователя по имени пользователя
        public async Task<UserModel> GetUserAsync(string username)
        {
            try
            {
                var query = _firestoreDb.Collection(UsersCollection)
                                        .WhereEqualTo("Username", username);
                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count > 0)
                {
                    var doc = snapshot.Documents[0];
                    Console.WriteLine($"Пользователь {username} найден.");
                    return doc.ConvertTo<UserModel>();
                }

                Console.WriteLine($"Пользователь {username} не найден.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении пользователя: {ex.Message}");
                throw;
            }
        }



        // Метод для получения текущего пользователя
        public async Task<UserModel> GetCurrentUserAsync()
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                Console.WriteLine("ID текущего пользователя отсутствует.");
                return null;
            }

            try
            {
                var docRef = _firestoreDb.Collection(UsersCollection).Document(CurrentUserId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    Console.WriteLine($"Пользователь с ID {CurrentUserId} успешно загружен.");
                    return snapshot.ConvertTo<UserModel>();
                }
                else
                {
                    Console.WriteLine($"Пользователь с ID {CurrentUserId} не найден.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке данных пользователя: {ex.Message}");
                MessageBox.Show($"Ошибка при загрузке данных пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // Метод для сохранения данных пользователя
        public async Task SaveUserAsync(UserModel user)
        {
            if (user == null)
            {
                Console.WriteLine("Попытка сохранить пустого пользователя.");
                return;
            }

            try
            {
                var docRef = _firestoreDb.Collection(UsersCollection).Document(user.Id.ToString());
                await docRef.SetAsync(user);
                CurrentUserId = user.Id.ToString(); // Сохраняем ID текущего пользователя
                Console.WriteLine($"Пользователь с ID {user.Id} успешно сохранен.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении данных пользователя: {ex.Message}");
                MessageBox.Show($"Ошибка при сохранении данных пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
