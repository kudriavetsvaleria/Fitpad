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
        private static string DayId(DateTime dayLocal) => dayLocal.ToString("yyyy-MM-dd");
        public FirestoreService()
        {
            string pathToKeyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "fitpad-2025-320d3ddb471a.json");
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

        /// <summary>
        /// Обновляет поле Title у записи дневника питания за указанную дату.
        /// Совпадение ищется по Date (+Time, если задан), а также по Weight и Calories.
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="dateLocal">Локальная дата (сегодня) — будет приведена к "yyyy-MM-dd"</param>
        /// <param name="item">Запись (NutritionModel), из которой берём Time/Weight/Calories</param>
        /// <param name="newTitle">Новое название; если null — возьмём item.Title</param>
        /// <param name="updateAllMatches">true — обновить все найденные совпадения; false — только первое</param>
        public async Task<int> UpdateFoodDiaryEntryTitleAsync(
            string userId,
            DateTime dateLocal,
            NutritionModel item,
            string newTitle = null,
            bool updateAllMatches = false)
        {
            if (string.IsNullOrWhiteSpace(userId) || item == null)
                return 0;

            var db = GetFirestoreDb();
            var diaryRef = db.Collection("Users").Document(userId).Collection("FoodDiary");

            string dateStr = dateLocal.ToString("yyyy-MM-dd");

            // Строим запрос БЕЗ фильтра по старому Title (он как раз может быть на EN)
            Query query = diaryRef
                .WhereEqualTo("Date", dateStr)
                .WhereEqualTo("Weight", item.Weight)
                .WhereEqualTo("Calories", item.Calories);

            if (!string.IsNullOrWhiteSpace(item.Time))
                query = query.WhereEqualTo("Time", item.Time);

            var snap = await query.GetSnapshotAsync();
            if (snap.Count == 0) return 0;

            string titleToSet = string.IsNullOrWhiteSpace(newTitle) ? item.Title : newTitle;

            int updated = 0;
            foreach (var doc in snap.Documents)
            {
                await doc.Reference.UpdateAsync(new Dictionary<string, object>
                {
                    ["Title"] = titleToSet
                });
                updated++;

                if (!updateAllMatches) break; // по умолчанию — только первое совпадение
            }

            return updated;
        }

        public async Task BackfillDaySummariesFromRegistrationAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            var createdAt = await GetUserCreatedAtAsync(userId);
            if (createdAt == null) return;

            var startLocal = createdAt.Value.ToDateTime();         // UTC -> DateTime
            startLocal = startLocal.ToLocalTime().Date;            // в локальную дату
            var todayLocal = DateTime.Now.Date;

            for (var day = startLocal; day <= todayLocal; day = day.AddDays(1))
            {
                // пробуем пересчитать из дневника
                var summary = await RecomputeDaySummaryAsync(userId, day);

                // если записей не было — создадим пустую сводку
                if (summary != null && summary.ItemsCount > 0) continue;
                await UpsertEmptySummaryIfMissingAsync(userId, day);
            }
        }

        public async Task<Timestamp?> GetUserCreatedAtAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var userDoc = await _firestoreDb.Collection("Users").Document(userId).GetSnapshotAsync();
            if (!userDoc.Exists) return null;

            var user = userDoc.ConvertTo<UserModel>();
            return user?.CreatedAt;
        }

        private async Task UpsertEmptySummaryIfMissingAsync(string userId, DateTime dayLocal)
        {
            var dayStr = DayId(dayLocal);
            var docRef = _firestoreDb
                .Collection("Users").Document(userId)
                .Collection("DaySummaries").Document(dayStr);

            var snap = await docRef.GetSnapshotAsync();
            if (snap.Exists) return;

            var empty = new DaySummaryModel
            {
                Date = dayStr,
                Calories = 0,
                Protein = 0,
                Fats = 0,
                Carbs = 0,
                Water = 0,
                ItemsCount = 0
            };
            await docRef.SetAsync(empty);
        }



        // ===================== FoodDiary =====================

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
                { "Timestamp", Timestamp.FromDateTime(whenUtc) },

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

        // ===================== DaySummaries =====================

        /// <summary>Получить сводку за день (если есть), без пересчёта.</summary>
        public async Task<DaySummaryModel> GetDaySummaryAsync(string userId, DateTime dayLocal)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var doc = await _firestoreDb
                .Collection("Users").Document(userId)
                .Collection("DaySummaries").Document(DayId(dayLocal))
                .GetSnapshotAsync();

            return doc.Exists ? doc.ConvertTo<DaySummaryModel>() : null;
        }

        /// <summary>Вернуть список сводок в диапазоне дат (включительно).</summary>
        public async Task<List<DaySummaryModel>> GetDaySummariesAsync(string userId, DateTime fromLocal, DateTime toLocal)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new List<DaySummaryModel>();

            var col = _firestoreDb.Collection("Users").Document(userId).Collection("DaySummaries");
            var start = DayId(fromLocal.Date);
            var end = DayId(toLocal.Date);

            var snap = await col
                .WhereGreaterThanOrEqualTo(FieldPath.DocumentId, start)
                .WhereLessThanOrEqualTo(FieldPath.DocumentId, end)
                .OrderBy(FieldPath.DocumentId)
                .GetSnapshotAsync();

            return snap.Documents.Select(d => d.ConvertTo<DaySummaryModel>()).ToList();
        }

        /// <summary>Пересчитать сводку за день из коллекции FoodDiary (источник истины).</summary>
        public async Task<DaySummaryModel> RecomputeDaySummaryAsync(string userId, DateTime dayLocal)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var dayStr = DayId(dayLocal);

            var diaryRef = _firestoreDb.Collection("Users").Document(userId).Collection("FoodDiary");
            var snap = await diaryRef.WhereEqualTo("Date", dayStr).GetSnapshotAsync();

            double calories = 0, protein = 0, fats = 0, carbs = 0, water = 0;
            int items = 0;

            foreach (var doc in snap.Documents)
            {
                var d = doc.ToDictionary();

                double Get(string key) => d.TryGetValue(key, out var v) ? Convert.ToDouble(v) : 0;

                calories += Get("Calories");
                protein += Get("Protein");
                fats += Get("Fats");
                carbs += Get("Carbs");
                water += Get("Water");
                items++;
            }

            var summary = new DaySummaryModel
            {
                Date = dayStr,
                Calories = Math.Round(calories, 1),
                Protein = Math.Round(protein, 1),
                Fats = Math.Round(fats, 1),
                Carbs = Math.Round(carbs, 1),
                Water = Math.Round(water, 1),
                ItemsCount = items
            };

            await _firestoreDb.Collection("Users").Document(userId)
                .Collection("DaySummaries").Document(dayStr)
                .SetAsync(summary);

            return summary;
        }

        public Task<DaySummaryModel> RecomputeTodayAsync(string userId)
            => RecomputeDaySummaryAsync(userId, DateTime.Now.Date);
    


        // ===================== UserProducts =====================

        public async Task SaveUserProductAsync(string userId, NutritionModel portion)
        {
            if (string.IsNullOrWhiteSpace(userId) || portion == null) return;

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
            await col.Document(catalog.Id).SetAsync(catalog);
        }

        private static string NormalizeKey(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return Guid.NewGuid().ToString();
            var key = title.Trim().ToLower();
            key = new string(key.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_').ToArray());
            return string.IsNullOrEmpty(key) ? Guid.NewGuid().ToString() : key;
        }

        // ===================== Dishes =====================

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
                Console.WriteLine($"Помилка під час завантаження страв: {ex.Message}");
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
                Console.WriteLine($"Найдено {snapshot.Count} блюд.");
        }

        // ===================== Users / UserInfos =====================

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
            if (string.IsNullOrWhiteSpace(userId))
            {
                Console.WriteLine("GetUserInfoAsync: userId is empty/null — return null, skip Firestore.");
                return null;
            }

            var docRef = _firestoreDb.Collection("UserInfos").Document(userId);
            var snapshot = await docRef.GetSnapshotAsync();

            return snapshot.Exists ? snapshot.ConvertTo<UserInfoModel>() : null;
        }

    }
}
