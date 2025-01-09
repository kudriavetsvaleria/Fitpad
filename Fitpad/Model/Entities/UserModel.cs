using Google.Cloud.Firestore;

[FirestoreData]
public class UserModel
{
    [FirestoreProperty]
    public string Id { get; set; }

    [FirestoreProperty]
    public string Name { get; set; }

    [FirestoreProperty]
    public string Email { get; set; }

    [FirestoreProperty]
    public string Password { get; set; }
}
