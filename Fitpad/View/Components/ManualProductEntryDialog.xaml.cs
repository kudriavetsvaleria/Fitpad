using Fitpad.Model.Entities;
using System;
using System.Windows;

namespace Fitpad.View.Components
{
    public partial class ManualProductEntryDialog : Window
    {
        public NutritionModel EnteredProduct { get; private set; }

        private readonly string _productName;
        private readonly double _weight;

        public ManualProductEntryDialog(string productName, double weight)
        {
            InitializeComponent();
            _productName = productName;
            _weight = weight;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(CaloriesInput.Text, out double calories) ||
                !double.TryParse(ProteinInput.Text, out double protein) ||
                !double.TryParse(FatsInput.Text, out double fats) ||
                !double.TryParse(CarbsInput.Text, out double carbs))
            {
                MessageBox.Show("Будь ласка, введіть коректні числові значення!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EnteredProduct = new NutritionModel
            {
                Title = _productName,
                Weight = _weight,
                Calories = calories,
                Protein = protein,
                Fats = fats,
                Carbs = carbs,
                Time = DateTime.Now.ToString("HH:mm")
            };

            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }



        public NutritionModel GetEnteredProduct()
        {
            return EnteredProduct;
        }
    }
}
