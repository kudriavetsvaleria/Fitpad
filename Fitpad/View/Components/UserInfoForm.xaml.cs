using System.Windows;
using System.Windows.Controls;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using System.Threading.Tasks;
using System;

namespace Fitpad.View.Components
{
    public partial class UserInfoForm : UserControl
    {
        private readonly UserInfoRepository _userInfoRepository;
        private readonly int _userId;

        // Публичный конструктор без параметров
        public UserInfoForm()
        {
            InitializeComponent();
            _userInfoRepository = new UserInfoRepository(); // Инициализируем репозиторий
        }

        // Конструктор с параметром userId
        public UserInfoForm(int userId) : this()
        {
            _userId = userId;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем корректность введенных данных
                string gender = (GenderInput?.SelectedItem as ComboBoxItem)?.Content?.ToString();
                string ageText = AgeInput?.Text;
                string heightText = HeightInput?.Text;
                string weightText = WeightInput?.Text;
                string activityLevel = (ActivityLevelInput?.SelectedItem as ComboBoxItem)?.Content?.ToString();

                if (string.IsNullOrWhiteSpace(gender) || string.IsNullOrWhiteSpace(activityLevel))
                {
                    MessageBox.Show("Пожалуйста, выберите пол и уровень активности.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(ageText, out int age) || !int.TryParse(heightText, out int height) || !double.TryParse(weightText, out double weight))
                {
                    MessageBox.Show("Ошибка: Проверьте корректность введенных данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var userInfo = new UserInfoModel
                {
                    UserId = _userId.ToString(), // Преобразуем int в string
                    Gender = gender,
                    Age = age,
                    Height = height,
                    Weight = weight,
                    ActivityLevel = activityLevel,
                    Purpose = "Поддержание формы"
                };

                await _userInfoRepository.SaveUserInfoAsync(userInfo);
                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
