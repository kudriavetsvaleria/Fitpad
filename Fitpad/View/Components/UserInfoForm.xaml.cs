using System;
using System.Windows;
using System.Windows.Controls;
using Fitpad.Model;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad.View.Components
{
    public partial class UserInfoForm : UserControl
    {
        private readonly UserInfoFormViewModel _viewModel;

        // Публичный конструктор без параметров
        public UserInfoForm()
        {
            InitializeComponent();
        }

        // Конструктор с параметром UserModel
        public UserInfoForm(UserModel currentUser) : this() // Вызываем конструктор без параметров
        {
            _viewModel = new UserInfoFormViewModel(currentUser);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string gender = (GenderInput.SelectedItem as ComboBoxItem)?.Content.ToString();
            string ageText = AgeInput.Text;
            string heightText = HeightInput.Text;
            string weightText = WeightInput.Text;
            string activityLevel = (ActivityLevelInput.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (_viewModel.SaveUserInfo(gender, ageText, heightText, weightText, activityLevel))
            {
                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Visibility = Visibility.Collapsed; // Скрываем форму после успешного сохранения
            }
            else
            {
                MessageBox.Show("Ошибка при сохранении данных. Проверьте введенные значения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
