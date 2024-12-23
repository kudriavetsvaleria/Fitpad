using Fitpad.Model.Entities;

namespace Fitpad.Model.Repositories
{
    public class CaloriesRepository
    {
        public CaloriesModel CalculateCalories(double weight, double height, int age, string gender, double activityLevel)
        {
            double bmr;
            if (gender == "Male")
            {
                bmr = 10 * weight + 6.25 * height - 5 * age + 5;
            }
            else
            {
                bmr = 10 * weight + 6.25 * height - 5 * age - 161;
            }

            var totalCalories = bmr * activityLevel;
            return new CaloriesModel
            {
                Calories = totalCalories,
                Protein = totalCalories * 0.3 / 4,
                Fats = totalCalories * 0.3 / 9,
                Carbs = totalCalories * 0.4 / 4
            };
        }
    }
}
