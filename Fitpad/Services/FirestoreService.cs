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


        public async Task<List<DishModel>> GetFavoriteDishes(string userId)
        {
            try
            {
                var querySnapshot = await _firestoreDb.Collection("dishes").Document(userId).Collection("userDishes")
                                        .WhereEqualTo("IsFavorite", true)
                                        .GetSnapshotAsync();

                List<DishModel> favoriteDishes = new List<DishModel>();

                foreach (var document in querySnapshot.Documents)
                {
                    DishModel dish = document.ConvertTo<DishModel>();
                    favoriteDishes.Add(dish);
                }

                Console.WriteLine($"Найдено {favoriteDishes.Count} избранных блюд.");
                return favoriteDishes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении избранных блюд: {ex.Message}");
                return new List<DishModel>();
            }
        }

        public async Task SaveDishToFirebase(DishModel dish)
        {
            try
            {
                FirestoreDb db = FirestoreDb.Create("fitpad-2025"); // Создание подключения
                CollectionReference dishesRef = db.Collection("dishes");

                // Создаём новый документ в коллекции "dishes"
                DocumentReference newDishRef = dishesRef.Document(dish.Id);
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
            List<DishModel> dishes = new List<DishModel>();

            try
            {
                FirestoreDb db = FirestoreDb.Create("fitpad-2025");

                // Запрос всех блюд, где UserId = userId
                Query dishesQuery = db.Collection("dishes").WhereEqualTo("UserId", userId);
                QuerySnapshot snapshot = await dishesQuery.GetSnapshotAsync();

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        DishModel dish = doc.ConvertTo<DishModel>();
                        dishes.Add(dish);
                    }
                }

                Console.WriteLine($"✅ Загружено {dishes.Count} блюд для пользователя {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при загрузке блюд: {ex.Message}");
            }

            return dishes;
        }

        public async Task UpdateFavoriteStatus(string dishId, bool isFavorite)
        {
            if (string.IsNullOrWhiteSpace(dishId))
            {
                Console.WriteLine("❌ Ошибка: ID блюда пустой!");
                return;
            }

            Console.WriteLine($"🔥 Обновляем избранное в Firestore: {dishId}, IsFavorite: {isFavorite}");

            var db = FirestoreDb.Create("fitpad-2025");
            var dishRef = db.Collection("Dishes").Document(dishId);

            await dishRef.UpdateAsync(new Dictionary<string, object>
    {
        { "IsFavorite", isFavorite }
    });

            Console.WriteLine("✅ Избранное успешно обновлено в Firestore!");
        }

        public string GenerateDishId()
        {
            return Guid.NewGuid().ToString(); // Генерирует уникальный ID
        }



        public async Task CheckDishesCollection()
        {
            var db = FirestoreDb.Create("fitpad-2025"); // Подключаемся к Firestore
            CollectionReference dishesRef = db.Collection("dishes");

            QuerySnapshot snapshot = await dishesRef.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                Console.WriteLine("❌ Коллекция `dishes` пустая или не существует!");
            }
            else
            {
                Console.WriteLine($"✅ Найдено {snapshot.Documents.Count} блюд в коллекции `dishes`.");
                foreach (var doc in snapshot.Documents)
                {
                    Console.WriteLine($"🔹 Блюдо ID: {doc.Id} - {doc.ToDictionary()}");
                }
            }
        }

    }
}
