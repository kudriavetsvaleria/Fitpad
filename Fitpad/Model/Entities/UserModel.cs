using Google.Cloud.Firestore;

namespace Fitpad.Model.Entities
{
    [FirestoreData]
    public class UserModel
    {
        [FirestoreProperty]
        public int Id { get; set; }

        [FirestoreProperty]
        public string Username { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string Password { get; set; }
    }
}
