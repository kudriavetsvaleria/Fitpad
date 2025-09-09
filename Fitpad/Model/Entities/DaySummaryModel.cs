using Google.Cloud.Firestore;

namespace Fitpad.Model.Entities
{
    [FirestoreData]
    public class DaySummaryModel
    {
        public DaySummaryModel() { }

        [FirestoreProperty] public string Date { get; set; } = "";
        [FirestoreProperty] public double Calories { get; set; }
        [FirestoreProperty] public double Protein { get; set; }
        [FirestoreProperty] public double Fats { get; set; }
        [FirestoreProperty] public double Carbs { get; set; }
        [FirestoreProperty] public double Water { get; set; }
        [FirestoreProperty] public int ItemsCount { get; set; }
    }
}
