using System;
using System.Windows;
using Fitpad.Model.Entities;

namespace Fitpad.View
{
    public partial class ManualProductEntryDialog : Window
    {
        public NutritionModel CreatedProduct { get; private set; }

        private readonly string _productName;
        private readonly double _weight;

        public ManualProductEntryDialog(string productName, double weight)
        {
            InitializeComponent();
            _productName = productName;
            _weight = weight;

            // эти поля должны быть в XAML с x:Name
            ProductNameText.Text = _productName;
            WeightText.Text = $"{_weight}";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(CaloriesInput.Text, out double cal) &&
                double.TryParse(ProteinInput.Text, out double prot) &&
                double.TryParse(FatsInput.Text, out double fat) &&
                double.TryParse(CarbsInput.Text, out double carb))
            {
                CreatedProduct = new NutritionModel
                {
                    Title = _productName,
                    Weight = _weight,
                    Calories = cal,
                    Protein = prot,
                    Fats = fat,
                    Carbs = carb,
                    Time = DateTime.Now.ToString("HH:mm")
                };

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Будь ласка, введіть коректні числові значення!",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
