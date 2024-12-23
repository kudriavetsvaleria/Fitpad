using Fitpad.ViewModel.PagesViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Fitpad.View.Pages
{
    public partial class CaloriesPage : Page
    {
        private readonly CaloriesViewModel _viewModel;

        public CaloriesPage()
        {
            InitializeComponent(); // Инициализация компонентов из XAML
            _viewModel = new CaloriesViewModel();
            DataContext = _viewModel; // Установка контекста данных для привязки
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка и конвертация введенных данных
                if (double.TryParse(WeightInput.Text, out var weight) &&
                    double.TryParse(HeightInput.Text, out var height) &&
                    int.TryParse(AgeInput.Text, out var age) &&
                    double.TryParse(ActivityLevelInput.Text, out var activityLevel))
                {
                    string gender = (GenderInput.SelectedItem as ComboBoxItem)?.Content.ToString();
                    if (string.IsNullOrWhiteSpace(gender))
                    {
                        MessageBox.Show("Выберите пол.");
                        return;
                    }

                    // Вызов метода расчета
                    _viewModel.Calculate(weight, height, age, gender, activityLevel);
                }
                else
                {
                    MessageBox.Show("Введите корректные значения во все поля.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }
    }
}
