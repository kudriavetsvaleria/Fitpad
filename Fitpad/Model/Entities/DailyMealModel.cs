using Google.Cloud.Firestore;
using System.Collections.Generic;

[FirestoreData]
public class DailyMealModel
{
    [FirestoreProperty]
    public string UserId { get; set; }

    [FirestoreProperty]
    public string Date { get; set; }

    [FirestoreProperty]
    public Dictionary<string, List<MealItem>> Meals { get; set; }
}

[FirestoreData]
public class MealItem
{
    [FirestoreProperty]
    public string Name { get; set; }

    [FirestoreProperty]
    public double Calories { get; set; }

    [FirestoreProperty]
    public double Proteins { get; set; }

    [FirestoreProperty]
    public double Fats { get; set; }

    [FirestoreProperty]
    public double Carbs { get; set; }
}
