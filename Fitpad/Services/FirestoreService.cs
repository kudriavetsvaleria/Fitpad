using Fitpad.Model.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Auth;
using Grpc.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Fitpad.Services
{
    public class FirestoreService
    {
        private readonly FirestoreDb _firestoreDb;

        // Конструктор, инициализирующий соединение с Firestore
        public FirestoreService()
        {
            // Абсолютный путь к файлу с учетными данными
            string pathToKeyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "fitpad-2025-firebase-adminsdk-orbvr-c6e144b386.json");
            Console.WriteLine($"Path to key file: {pathToKeyFile}");
            if (!File.Exists(pathToKeyFile))
            {
                throw new FileNotFoundException($"Файл учетных данных не найден по пути: {pathToKeyFile}");
            }

            if (!File.Exists(pathToKeyFile))
            {
                throw new FileNotFoundException($"Файл ключа не найден: {pathToKeyFile}");
            }

            // Устанавливаем переменную среды для Google Cloud SDK
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToKeyFile);
            Console.WriteLine($"GOOGLE_APPLICATION_CREDENTIALS set to: {Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")}");
            // Создаем учетные данные Google и инициализируем Firestore
            GoogleCredential credential = GoogleCredential.FromFile(pathToKeyFile);
            ChannelCredentials channelCredentials = credential.ToChannelCredentials();

            _firestoreDb = new FirestoreDbBuilder
            {
                ProjectId = "fitpad-2025",
                ChannelCredentials = channelCredentials
            }.Build();

            Console.WriteLine("Соединение с Firestore установлено.");
        }

        // Метод для получения экземпляра FirestoreDb
        public FirestoreDb GetFirestoreDb()
        {
            return _firestoreDb;
        }

        public async Task SaveUserAsync(UserModel user)
        {
            try
            {
                DocumentReference docRef = _firestoreDb.Collection("Users").Document(user.Id);
                await docRef.SetAsync(user); // Убедитесь, что у объекта user есть данные
                Console.WriteLine("Пользователь успешно сохранен.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении пользователя: {ex.Message}");
                throw;
            }
        }


        // Пример метода для сохранения данных пользователя
        public async Task SaveUserInfoAsync(UserInfoModel userInfo)
        {
            try
            {
                DocumentReference docRef = _firestoreDb.Collection("UserInfos").Document(userInfo.UserId);
                await docRef.SetAsync(userInfo);
                Console.WriteLine("Информация о пользователе успешно сохранена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении информации о пользователе: {ex.Message}");
                throw;
            }
        }

        // Пример метода для загрузки данных пользователя
        public async Task<UserInfoModel> GetUserInfoAsync(string userId)
        {
            try
            {
                DocumentReference docRef = _firestoreDb.Collection("UserInfos").Document(userId);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    Console.WriteLine("Информация о пользователе успешно загружена.");
                    return snapshot.ConvertTo<UserInfoModel>();
                }

                Console.WriteLine("Информация о пользователе не найдена.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке информации о пользователе: {ex.Message}");
                throw;
            }
        }

        public async Task<DailyMealModel> GetDailyMealAsync(string userId, string date)
        {
            try
            {
                string documentId = $"{userId}_{date}";
                DocumentReference docRef = _firestoreDb.Collection("Meals").Document(documentId);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    Console.WriteLine("Данные о питании успешно загружены.");
                    return snapshot.ConvertTo<DailyMealModel>();
                }

                Console.WriteLine("Данные о питании не найдены.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке данных о питании: {ex.Message}");
                throw;
            }
        }

        public async Task<DailyMealModel> GetDailyMealsAsync(string userId, string date)
        {
            try
            {
                DocumentReference docRef = _firestoreDb.Collection("DailyMeals").Document($"{userId}_{date}");
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    return snapshot.ConvertTo<DailyMealModel>();
                }
                else
                {
                    Console.WriteLine("Данные не найдены.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения данных: {ex.Message}");
                throw;
            }
        }

        public async Task SaveDailyMealAsync(DailyMealModel dailyMeal)
        {
            try
            {
                string documentId = $"{dailyMeal.UserId}_{dailyMeal.Date}";
                DocumentReference docRef = _firestoreDb.Collection("Meals").Document(documentId);
                await docRef.SetAsync(dailyMeal);
                Console.WriteLine("Данные о питании успешно сохранены.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении данных о питании: {ex.Message}");
                throw;
            }
        }



    }
}
