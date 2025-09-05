using Fitpad.Model.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Auth;
using Grpc.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            string pathToKeyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "fitpad-2025-91f9ea3e1402.json");


            Console.WriteLine($"🔍 Ожидаемый путь к файлу учетных данных: {pathToKeyFile}");

            if (!File.Exists(pathToKeyFile))
            {
                Console.WriteLine($"❌ Файл НЕ найден: {pathToKeyFile}");
                throw new FileNotFoundException($"Файл учетных данных не найден по пути: {pathToKeyFile}");
            }

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToKeyFile);
            Console.WriteLine($"✅ GOOGLE_APPLICATION_CREDENTIALS установлен в: {Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")}");


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

        public async Task DeleteDishFromFirebase(string userId, string dishId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(dishId))
            {
                Console.WriteLine("❌ Ошибка: пустой userId или dishId.");
                return;
            }

            var dishRef = _firestoreDb.Collection("Users").Document(userId)
                                      .Collection("Dishes").Document(dishId);

            DocumentSnapshot snapshot = await dishRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                Console.WriteLine($"❌ Ошибка: Блюдо с ID {dishId} не найдено!");
                return;
            }

            await dishRef.DeleteAsync();
            Console.WriteLine($"✅ Блюдо {dishId} успешно удалено!");
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

        public async Task<List<NutritionModel>> GetUserProductsAsync(string userId)
        {
            List<NutritionModel> products = new List<NutritionModel>();

            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("❌ Ошибка: UserID не найден.");
                    return products;
                }

                FirestoreDb db = FirestoreDb.Create("fitpad-2025");
                CollectionReference userProductsRef = db.Collection("Users").Document(userId).Collection("UserProducts");

                QuerySnapshot snapshot = await userProductsRef.GetSnapshotAsync();

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        var productData = doc.ToDictionary();
                        NutritionModel product = new NutritionModel
                        {
                            Title = productData.ContainsKey("Title") ? productData["Title"].ToString() : "",
                            Weight = productData.ContainsKey("Weight") ? Convert.ToDouble(productData["Weight"]) : 0,
                            Calories = productData.ContainsKey("Calories") ? Convert.ToDouble(productData["Calories"]) : 0,
                            Protein = productData.ContainsKey("Protein") ? Convert.ToDouble(productData["Protein"]) : 0,
                            Fats = productData.ContainsKey("Fats") ? Convert.ToDouble(productData["Fats"]) : 0,
                            Carbs = productData.ContainsKey("Carbs") ? Convert.ToDouble(productData["Carbs"]) : 0,
                            Time = productData.ContainsKey("Time") ? productData["Time"].ToString() : DateTime.Now.ToString("HH:mm")
                        };
                        products.Add(product);
                    }
                }

                Console.WriteLine($"✅ Загружено {products.Count} продуктов для пользователя {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при загрузке продуктов: {ex.Message}");
            }

            return products;
        }


        public async Task SaveUserProductAsync(string userId, NutritionModel product)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("❌ [Firestore] Ошибка: UserID пустой! Продукт не будет сохранён.");
                    return;
                }

                if (product == null)
                {
                    Console.WriteLine("❌ [Firestore] Ошибка: Продукт = null! Сохранение отменено.");
                    return;
                }

                FirestoreDb db = FirestoreDb.Create("fitpad-2025");
                CollectionReference userProductsRef = db.Collection("Users").Document(userId).Collection("UserProducts");

                DocumentReference newProductRef = userProductsRef.Document(Guid.NewGuid().ToString());

                Console.WriteLine($"🟢 [Firestore] Попытка сохранить продукт: {product.Title} ({product.Weight}г, {product.Calories} ккал) для пользователя {userId}");

                await newProductRef.SetAsync(product);

                Console.WriteLine($"✅ [Firestore] Продукт '{product.Title}' успешно сохранён в Firestore!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Firestore] Ошибка при сохранении продукта в Firestore: {ex.Message}");
            }
        }

        public async Task<List<DishModel>> GetUserDishesAsync(string userId)
        {
            var dishes = new List<DishModel>();

            try
            {
                var snapshot = await _firestoreDb
                    .Collection("Users").Document(userId)
                    .Collection("Dishes")
                    .GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    if (doc.Exists)
                        dishes.Add(doc.ConvertTo<DishModel>());
                }

                Console.WriteLine($"✅ Завантажено {dishes.Count} страв для користувача {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Помилка під час завантаження страв: {ex.Message}");
            }

            return dishes;
        }


        public async Task SaveDishToFirebase(string userId, DishModel dish)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || dish == null || string.IsNullOrWhiteSpace(dish.Id))
                {
                    Console.WriteLine("❌ Ошибка: некорректные userId/dish.");
                    return;
                }

                var dishesRef = _firestoreDb.Collection("Users").Document(userId).Collection("Dishes");
                var newDishRef = dishesRef.Document(dish.Id);
                await newDishRef.SetAsync(dish);

                Console.WriteLine($"✅ Блюдо '{dish.Name}' сохранено в Firestore (ID: {dish.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при сохранении блюда: {ex.Message}");
            }
        }


        public async Task<List<DishModel>> GetUserDishes(string userId)
        {
            var dishes = new List<DishModel>();

            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    Console.WriteLine("❌ Ошибка: userId пустой.");
                    return dishes;
                }

                var snapshot = await _firestoreDb.Collection("Users").Document(userId)
                                                 .Collection("Dishes").GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    if (doc.Exists)
                        dishes.Add(doc.ConvertTo<DishModel>());
                }

                Console.WriteLine($"✅ Загружено {dishes.Count} блюд.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при загрузке блюд: {ex.Message}");
            }

            return dishes;
        }

        public async Task UpdateFavoriteStatus(string userId, string dishId, bool isFavorite)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(dishId))
            {
                Console.WriteLine("❌ Ошибка: пустой userId или dishId!");
                return;
            }

            var dishRef = _firestoreDb.Collection("Users").Document(userId)
                                      .Collection("Dishes").Document(dishId);

            var snapshot = await dishRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                Console.WriteLine($"❌ Ошибка: Блюдо с ID {dishId} не найдено в Firestore!");
                return;
            }

            Console.WriteLine($"🔥 Обновляем статус избранного: {dishId}, IsFavorite: {isFavorite}");
            await dishRef.UpdateAsync(new Dictionary<string, object> { { "IsFavorite", isFavorite } });
            Console.WriteLine("✅ Избранное успешно обновлено в Firestore!");
        }



        public string GenerateDishId()
        {
            return Guid.NewGuid().ToString(); // Генерирует уникальный ID
        }


        public async Task UpdateDishFavoriteStatus(string userId, string dishId, bool isFavorite)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(dishId))
            {
                Console.WriteLine("❌ Ошибка: userId или dishId пустой!");
                return;
            }

            try
            {
                var dishRef = _firestoreDb
                    .Collection("Users").Document(userId)
                    .Collection("Dishes").Document(dishId);

                await dishRef.UpdateAsync(new Dictionary<string, object>
        {
            { "IsFavorite", isFavorite },
            { "UpdatedAt", Timestamp.FromDateTime(DateTime.UtcNow) }
        });

                Console.WriteLine($"✅ Статус избранного для {dishId} обновлён: {isFavorite}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при обновлении статуса избранного: {ex.Message}");
            }
        }

        public async Task<List<DishModel>> GetFavoriteDishes(string userId)
        {
            var list = new List<DishModel>();
            var snapshot = await _firestoreDb
                .Collection("Users").Document(userId)
                .Collection("Dishes")
                .WhereEqualTo("IsFavorite", true)
                .GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
                list.Add(doc.ConvertTo<DishModel>());

            return list;
        }


        public async Task CheckDishesCollection(string userId)
        {
            var dishesRef = _firestoreDb.Collection("Users").Document(userId).Collection("Dishes");
            var snapshot = await dishesRef.GetSnapshotAsync();

            if (snapshot.Count == 0)
            {
                Console.WriteLine("ℹ️ У пользователя нет блюд в `Users/{userId}/Dishes`.");
            }
            else
            {
                Console.WriteLine($"✅ Найдено {snapshot.Count} блюд.");
                foreach (var doc in snapshot.Documents)
                    Console.WriteLine($"🔹 {doc.Id} -> {doc.ToDictionary()}");
            }
        }


    }
}
