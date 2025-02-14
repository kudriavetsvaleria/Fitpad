using System;
using System.Collections.Generic;

namespace Fitpad.Model.Entities
{
    public class NutritionModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Name { get; set; } // Название продукта
        public string Image { get; set; }
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fats { get; set; }
        public double Carbs { get; set; }
        public double Sugar { get; set; }
        public double Water { get; set; }
        public double Weight { get; set; } // Количество продукта в граммах
        public string Unit { get; set; } // Единица измерения
        public double Quantity { get; set; } // Количество продукта
        public string Time { get; set; } // Время приема пищи
        public int ReadyInMinutes { get; set; } // Время приготовления
        public string FormattedTime { get; set; }
        public string RecipeDetails { get; set; } // Инструкции рецепта
        public List<string> Ingredients { get; set; }

        public override string ToString()
        {
            return $"{Title} (Калории: {Calories}, Белки: {Protein}, Жиры: {Fats}, Углеводы: {Carbs})";
        }
    }
}
