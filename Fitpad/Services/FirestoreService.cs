using Fitpad.Model.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Auth;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fitpad.Services
{
    public class FirestoreService
    {
        private readonly FirestoreDb _firestoreDb;

        public FirestoreService()
        {
            string pathToKeyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "fitpad-2025-91f9ea3e1402.json");

            if (!File.Exists(pathToKeyFile))
                throw new FileNotFoundException($"Файл учетных данных не найден по пути: {pathToKeyFile}");

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToKeyFile);

            GoogleCredential credential = GoogleCredential.FromFile(pathToKeyFile);
            ChannelCredentials channelCredentials = credential.ToChannelCredentials();

            _firestoreDb = new FirestoreDbBuilder
            {
                ProjectId = "fitpad-2025",
                ChannelCredentials = channelCredentials
            }.Build();
        }

        public FirestoreDb GetFirestoreDb() => _firestoreDb;

        // ---- FoodDiary ----

        public async Task AddFoodDiaryEntryAsync(string userId, NutritionModel product, DateTime whenUtc)
        {
            if (string.IsNullOrWhiteSpace(userId) || product == null) return;

            var diaryRef = _firestoreDb
                .Collection("Users").Document(userId)
                .Collection("FoodDiary")
                .Document(Guid.NewGuid().ToString());

            var local = whenUtc.ToLocalTime();
            var dateStr = local.ToString("yyyy-MM-dd");
            var timeStr = local.ToString("HH:mm");

            var data = new Dictionary<string, object>
            {
                { "Date", dateStr },
                { "Time", timeStr },
                { "Timestamp", Timestamp.FromDateTime(whenUtc) }, // whenUtc ДОЛЖЕН быть UTC

                // значения порции (как показываешь в таблице)
                { "Title", product.Title ?? "" },
                { "Weight", product.Weight },
                { "Calories", product.Calories },
                { "Protein", product.Protein },
                { "Fats", product.Fats },
                { "Carbs", product.Carbs },
                { "Water", product.Water }
            };

            await diaryRef.SetAsync(data);
        }

        public async Task<List<NutritionModel>> GetFoodDiaryForDateAsync(string userId, DateTime dateLocal)
        {
            var list = new List<NutritionModel>();
            if (string.IsNullOrWhiteSpace(userId)) return list;

            var startLocal = dateLocal.Date;
            var endLocal = startLocal.AddDays(1);

            var startUtc = startLocal.ToUniversalTime();
            var endUtc = endLocal.ToUniversalTime();

            var snap = await _firestoreDb
                .Collection("Users").Document(userId)
                .Collection("FoodDiary")
                .WhereGreaterThanOrEqualTo("Timestamp", Timestamp.FromDateTime(startUtc))
                .WhereLessThan("Timestamp", Timestamp.FromDateTime(endUtc))
                .OrderBy("Timestamp")
                .GetSnapshotAsync();

            foreach (var doc in snap.Documents)
            {
                var d = doc.ToDictionary();
                list.Add(new NutritionModel
                {
                    Title = d.TryGetValue("Title", out var t) ? t?.ToString() : "",
                    Weight = d.TryGetValue("Weight", out var w) ? Convert.ToDouble(w) : 0,
                    Calories = d.TryGetValue("Calories", out var c) ? Convert.ToDouble(c) : 0,
                    Protein = d.TryGetValue("Protein", out var p) ? Convert.ToDouble(p) : 0,
                    Fats = d.TryGetValue("Fats", out var f) ? Convert.ToDouble(f) : 0,
                    Carbs = d.TryGetValue("Carbs", out var cb) ? Convert.ToDouble(cb) : 0,
                    Water = d.TryGetValue("Water", out var wa) ? Convert.ToDouble(wa) : 0,
                    Time = d.TryGetValue("Time", out var tm) ? tm?.ToString() : ""
                });
            }

            return list;
        }

        public async Task DeleteFoodDiaryEntryAsync(string userId, string diaryDocId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(diaryDocId)) return;

            await _firestoreDb
                .Collection("Users").Document(userId)
                .Collection("FoodDiary").Document(diaryDocId)
                .DeleteAsync();
        }

        // ---- UserProducts (каталог пользователя на 100 г) ----

        public async Task SaveUserProductAsync(string userId, NutritionModel portion)
        {
            if (string.IsNullOrWhiteSpace(userId) || portion == null) return;

            // нормализуем к 100 г
            var w = portion.Weight > 0 ? portion.Weight : 100.0;
            var k = 100.0 / w;

            var catalog = new NutritionModel
            {
                Id = NormalizeKey(portion.Title),
                Title = portion.Title,
                Name = portion.Name,
                Calories = Math.Round(portion.Calories * k, 2),
                Protein = Math.Round(portion.Protein * k, 2),
                Fats = Math.Round(portion.Fats * k, 2),
                Carbs = Math.Round(portion.Carbs * k, 2),
                Sugar = Math.Round(portion.Sugar * k, 2),
                Water = Math.Round(portion.Water * k, 2),
                DefaultServingGrams = 100
            };

            var col = _firestoreDb.Collection("Users").Document(userId).Collection("UserProducts");
            await col.Document(catalog.Id).SetAsync(catalog); // upsert без дублей
        }

        private static string NormalizeKey(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return Guid.NewGuid().ToString();
            var key = title.Trim().ToLower();
            key = new string(key.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_').ToArray());
            return string.IsNullOrEmpty(key) ? Guid.NewGuid().ToString() : key;
        }

        // ---- Dishes (как было) ----

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
                    if (doc.Exists) dishes.Add(doc.ConvertTo<DishModel>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Помилка під час завантаження страв: {ex.Message}");
            }
            return dishes;
        }

        public async Task SaveDishToFirebase(string userId, DishModel dish)
        {
            if (string.IsNullOrWhiteSpace(userId) || dish == null || string.IsNullOrWhiteSpace(dish.Id)) return;

            var dishesRef = _firestoreDb.Collection("Users").Document(userId).Collection("Dishes");
            await dishesRef.Document(dish.Id).SetAsync(dish);
        }

        public async Task UpdateFavoriteStatus(string userId, string dishId, bool isFavorite)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(dishId)) return;

            var dishRef = _firestoreDb.Collection("Users").Document(userId).Collection("Dishes").Document(dishId);
            var snapshot = await dishRef.GetSnapshotAsync();
            if (!snapshot.Exists) return;

            await dishRef.UpdateAsync(new Dictionary<string, object> { { "IsFavorite", isFavorite } });
        }

        public string GenerateDishId() => Guid.NewGuid().ToString();
        public async Task UpdateDishFavoriteStatus(string userId, string dishId, bool isFavorite)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(dishId)) return;

            var dishRef = _firestoreDb
                .Collection("Users").Document(userId)
                .Collection("Dishes").Document(dishId);

            await dishRef.UpdateAsync(new Dictionary<string, object>
            {
                { "IsFavorite", isFavorite },
                { "UpdatedAt", Timestamp.FromDateTime(DateTime.UtcNow) }
            });
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

        public async Task DeleteDishFromFirebase(string userId, string dishId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(dishId)) return;

            var dishRef = _firestoreDb
                .Collection("Users").Document(userId)
                .Collection("Dishes").Document(dishId);

            var snapshot = await dishRef.GetSnapshotAsync();
            if (!snapshot.Exists) return;

            await dishRef.DeleteAsync();
        }


        public async Task CheckDishesCollection(string userId)
        {
            var dishesRef = _firestoreDb.Collection("Users").Document(userId).Collection("Dishes");
            var snapshot = await dishesRef.GetSnapshotAsync();

            if (snapshot.Count == 0)
                Console.WriteLine("ℹ️ У пользователя нет блюд в `Users/{userId}/Dishes`.");
            else
                Console.WriteLine($"✅ Найдено {snapshot.Count} блюд.");
        }

        // ---- Users / UserInfos (как было) ----

        public async Task SaveUserAsync(UserModel user)
        {
            await _firestoreDb.Collection("Users").Document(user.Id).SetAsync(user);
        }

        public async Task SaveUserInfoAsync(UserInfoModel userInfo)
        {
            await _firestoreDb.Collection("UserInfos").Document(userInfo.UserId).SetAsync(userInfo);
        }

        public async Task<UserInfoModel> GetUserInfoAsync(string userId)
        {
            var doc = await _firestoreDb.Collection("UserInfos").Document(userId).GetSnapshotAsync();
            return doc.Exists ? doc.ConvertTo<UserInfoModel>() : null;
        }
    }
}
