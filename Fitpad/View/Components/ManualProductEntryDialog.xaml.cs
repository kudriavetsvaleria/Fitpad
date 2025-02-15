using Fitpad.Model.Entities;
using System.Windows;

namespace Fitpad.View.Components
{
    public partial class ManualProductEntryDialog : Window
    {
        public string ProductTitle { get; }
        public double Weight { get; }

        public double Calories => double.TryParse(CaloriesInput.Text, out double c) ? c : 0;
        public double Protein => double.TryParse(ProteinInput.Text, out double p) ? p : 0;
        public double Fats => double.TryParse(FatsInput.Text, out double f) ? f : 0;
        public double Carbs => double.TryParse(CarbsInput.Text, out double c) ? c : 0;

        public ManualProductEntryDialog(string productTitle, double weight)
        {
            InitializeComponent();
            ProductTitle = productTitle;
            Weight = weight;
        }

        public NutritionModel GetEnteredProduct()
        {
            return new NutritionModel
            {
                Title = ProductTitle,
                Weight = Weight,
                Calories = Calories,
                Protein = Protein,
                Fats = Fats,
                Carbs = Carbs
            };
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
