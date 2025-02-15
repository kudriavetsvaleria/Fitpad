using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace Fitpad.Model.Entities
{
    [FirestoreData]
    public class NutritionModel
    {
        [FirestoreProperty]
        public int Id { get; set; }
        [FirestoreProperty]
        public string Title { get; set; }
        [FirestoreProperty]
        public string Name { get; set; } // Название продукта
        [FirestoreProperty]
        public string Image { get; set; }
        [FirestoreProperty]
        public double Calories { get; set; }
        [FirestoreProperty]
        public double Protein { get; set; }
        [FirestoreProperty]
        public double Fats { get; set; }
        [FirestoreProperty]
        public double Carbs { get; set; }
        [FirestoreProperty]
        public double Sugar { get; set; }
        [FirestoreProperty]
        public double Water { get; set; }
        [FirestoreProperty]
        public double Weight { get; set; } // Количество продукта в граммах
        [FirestoreProperty]
        public string Unit { get; set; } // Единица измерения
        [FirestoreProperty]
        public double Quantity { get; set; } // Количество продукта
        [FirestoreProperty]
        public string Time { get; set; } // Время приема пищи
        [FirestoreProperty]
        public int ReadyInMinutes { get; set; } // Время приготовления
        [FirestoreProperty]
        public string FormattedTime { get; set; }
        [FirestoreProperty]
        public string RecipeDetails { get; set; } // Инструкции рецепта
        [FirestoreProperty]
        public List<string> Ingredients { get; set; }


        public override string ToString()
        {
            return $"{Title} (Калории: {Calories}, Белки: {Protein}, Жиры: {Fats}, Углеводы: {Carbs})";
        }
    }
}
