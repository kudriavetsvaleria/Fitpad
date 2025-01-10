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
        private readonly string _userId; 

        // Публичный конструктор без параметров
        public UserInfoForm()
        {
            InitializeComponent();
            _userInfoRepository = new UserInfoRepository(); // Инициализируем репозиторий
        }

        // Конструктор с параметром userId
        public UserInfoForm(UserModel user) : this()
        {
            _userInfoRepository = new UserInfoRepository();
            _userId = user.Id; // Получаем уникальный идентификатор пользователя
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string gender = (GenderInput?.SelectedItem as ComboBoxItem)?.Content?.ToString();
                string ageText = AgeInput?.Text;
                string heightText = HeightInput?.Text;
                string weightText = WeightInput?.Text;
                string activityLevel = (ActivityLevelInput?.SelectedItem as ComboBoxItem)?.Content?.ToString();
                string purpose = (PurposeInput?.SelectedItem as ComboBoxItem)?.Content?.ToString(); // Новое поле "Цель"

                if (string.IsNullOrWhiteSpace(gender) || string.IsNullOrWhiteSpace(activityLevel) || string.IsNullOrWhiteSpace(purpose))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля корректно.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(ageText, out int age) || !int.TryParse(heightText, out int height) || !double.TryParse(weightText, out double weight))
                {
                    MessageBox.Show("Ошибка: Проверьте корректность введенных данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var userInfo = new UserInfoModel
                {
                    UserId = _userId,
                    Gender = gender,
                    Age = age,
                    Height = height,
                    Weight = weight,
                    ActivityLevel = activityLevel,
                    Purpose = purpose // Сохраняем "Цель"
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
