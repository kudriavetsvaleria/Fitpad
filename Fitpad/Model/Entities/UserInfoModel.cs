using Google.Cloud.Firestore;

namespace Fitpad.Model.Entities
{
    [FirestoreData]
    public class UserInfoModel
    {
        [FirestoreProperty]
        public string UserId { get; set; } // Внешний ключ, связанный с Id из Users

        [FirestoreProperty]
        public string Gender { get; set; }

        [FirestoreProperty]
        public int Age { get; set; }

        [FirestoreProperty]
        public int Height { get; set; }

        [FirestoreProperty]
        public double Weight { get; set; }

        [FirestoreProperty]
        public string ActivityLevel { get; set; }

        [FirestoreProperty]
        public string Purpose { get; set; }
    }
}
