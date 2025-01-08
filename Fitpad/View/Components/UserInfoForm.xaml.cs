using System.Windows;
using System.Windows.Controls;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using System.Threading.Tasks;

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
        }

        // Конструктор с параметром userId
        public UserInfoForm(int userId) : this()
        {
            _userInfoRepository = new UserInfoRepository();
            _userId = userId;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string gender = (GenderInput?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string ageText = AgeInput?.Text;
            string heightText = HeightInput?.Text;
            string weightText = WeightInput?.Text;
            string activityLevel = (ActivityLevelInput?.SelectedItem as ComboBoxItem)?.Content?.ToString();

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
                Purpose = "Поддержание формы"
            };

            await _userInfoRepository.SaveUserInfoAsync(userInfo);
            MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Visibility = Visibility.Collapsed;
        }
    }
}

