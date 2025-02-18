using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks;
using Fitpad.Model.Entities;
using System.Windows;
using static Fitpad.Services.FirestoreService;
using Fitpad.Services;

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
                Console.WriteLine("Підключення до Firestore успішно встановлено.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка під час підключення до Firestore: {ex.Message}");
                MessageBox.Show($"Помилка підключення до бази даних: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public async Task<UserInfoModel> GetUserInfoAsync(string userId)
        {
            var firestoreService = new FirestoreService();
            return await firestoreService.GetUserInfoAsync(userId);
        }


        // Добавляем метод для получения пользователя по имени пользователя
        public async Task<UserModel> GetUserAsync(string username)
        {
            try
            {
                // Проверяем наличие пользователя с указанным именем
                var query = _firestoreDb.Collection("Users").WhereEqualTo("Name", username);
                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count > 0)
                {
                    var user = snapshot.Documents[0].ConvertTo<UserModel>();
                    Console.WriteLine($"Користувач знайдений: {user.Name}");
                    return user;
                }

                Console.WriteLine($"Користувач з ім'ям {username} не знайдений.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка під час пошуку користувача: {ex.Message}");
                throw;
            }
        }

        public async Task<UserModel> GetCurrentUserAsync()
        {
            // 🔹 Принудительно загружаем UserID, если он ещё пуст
            if (string.IsNullOrEmpty(UserSession.CurrentUserId))
            {
                Console.WriteLine("⚠️ UserSession.CurrentUserId порожній. Пробуємо завантажити з файлу...");
                UserSession.LoadUserIdFromFile();
            }

            if (string.IsNullOrEmpty(UserSession.CurrentUserId))
            {
                Console.WriteLine("❌ ID поточного користувача відсутній. Повертаємо NULL.");
                return null;
            }

            try
            {
                var docRef = _firestoreDb.Collection("Users").Document(UserSession.CurrentUserId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    Console.WriteLine($"✅ Користувач з ID {UserSession.CurrentUserId} успішно завантажений.");
                    return snapshot.ConvertTo<UserModel>();
                }
                else
                {
                    Console.WriteLine($"❌ Користувач з ID {UserSession.CurrentUserId} не знайдений. Повертаємо NULL.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Помилка під час завантаження даних користувача: {ex.Message}");
                return null;
            }
        }


        // Метод для сохранения данных пользователя
        public async Task SaveUserAsync(UserModel user)
        {
            try
            {
                var docRef = _firestoreDb.Collection(UsersCollection).Document(user.Id);
                await docRef.SetAsync(user);
                CurrentUserId = user.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Опомилка під час збереження користувача: {ex.Message}");
            }
        }
    }
}
