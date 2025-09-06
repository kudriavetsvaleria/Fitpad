using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace Fitpad.Model.Entities
{
    [FirestoreData]
    public class NutritionModel
    {
        // ---- Каталог (хранится в Firestore, значения на 100 г) ----
        [FirestoreProperty] public string Id { get; set; }
        [FirestoreProperty] public string Title { get; set; }
        [FirestoreProperty] public string Name { get; set; }

        // Не сохраняем картинку в БД
        public string Image { get; set; }

        // Пищевая ценность на 100 г
        [FirestoreProperty] public double Calories { get; set; }
        [FirestoreProperty] public double Protein { get; set; }
        [FirestoreProperty] public double Fats { get; set; }
        [FirestoreProperty] public double Carbs { get; set; }
        [FirestoreProperty] public double Sugar { get; set; }
        [FirestoreProperty] public double Water { get; set; }

        [FirestoreProperty] public double DefaultServingGrams { get; set; } = 100;

        // ---- Поля только для UI/калькулятора (НЕ сохранять в БД) ----
        public double Weight { get; set; }           // граммы выбранной порции
        public string Unit { get; set; }
        public double Quantity { get; set; }
        public double QuantityInGrams { get; set; }

        public string Time { get; set; }             // для отображения в таблице
        public int ReadyInMinutes { get; set; }
        public string FormattedTime { get; set; }
        public string RecipeDetails { get; set; }
        public List<string> Ingredients { get; set; }

        public override string ToString()
            => $"{Title} (Ккал: {Calories}, Б: {Protein}, Ж: {Fats}, У: {Carbs})";
    }
}
