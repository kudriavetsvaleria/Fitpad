namespace Fitpad.Model.Entities
{
    public class NutritionModel
    {
        public int Id { get; set; } // Идентификатор рецепта
        public string Title { get; set; }
        public string Image { get; set; }
        public int Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public int ReadyInMinutes { get; set; } // Время приготовления
        public string FormattedTime { get; set; }
        public string RecipeDetails { get; set; } // Инструкции рецепта
    }

}
