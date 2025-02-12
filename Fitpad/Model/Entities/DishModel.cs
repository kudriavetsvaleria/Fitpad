using Google.Cloud.Firestore;
using System.Collections.Generic;

[FirestoreData]
public class DishModel
{
    [FirestoreProperty]
    public string Id { get; set; }

    [FirestoreProperty]
    public string UserId { get; set; }

    [FirestoreProperty]
    public string Name { get; set; }

    [FirestoreProperty]
    public string CookingTime { get; set; }

    [FirestoreProperty]
    public string Recipe { get; set; }

    [FirestoreProperty]
    public List<string> Ingredients { get; set; }

    [FirestoreProperty]
    public bool IsFavorite { get; set; }
}
