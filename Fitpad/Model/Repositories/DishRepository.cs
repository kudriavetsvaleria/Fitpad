using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Fitpad.Model.Entities;

namespace Fitpad.Model.Repositories
{
    public class DishRepository
    {
        private readonly FirestoreDb _firestoreDb;

        public DishRepository(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<List<DishModel>> GetUserDishesAsync(string userId)
        {
            List<DishModel> dishes = new List<DishModel>();

            try
            {
                Query query = _firestoreDb.Collection("dishes").WhereEqualTo("UserId", userId);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        DishModel dish = doc.ConvertTo<DishModel>();
                        dishes.Add(dish);
                    }
                }

                Console.WriteLine($"✅ Завантажено {dishes.Count} страв для користувача {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Помилка під час завантаження страв: {ex.Message}");
            }

            return dishes;
        }
    }
}
