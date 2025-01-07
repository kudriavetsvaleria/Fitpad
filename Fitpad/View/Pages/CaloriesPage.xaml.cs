using System;
using System.Windows;
using System.Windows.Controls;
using Fitpad.Model;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad.View.Pages
{
    public partial class CaloriesPage : Page
    {
        private readonly UserInfoViewModel _viewModel;

        public CaloriesPage()
        {
            InitializeComponent();

            var currentUser = UserStorage.GetCurrentUser();
            if (currentUser == null)
            {
                MessageBox.Show("Пожалуйста, войдите в систему.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
                return;
            }

            _viewModel = new UserInfoViewModel(currentUser);
            DataContext = _viewModel;

            // Проверяем, заполнял ли пользователь анкету ранее
            if (_viewModel.HasUserInfo)
            {
                // Если анкета заполнена, показываем дату и день недели
                ShowDateAndDay();
            }
            else
            {
                // Если анкета не заполнена, показываем поля для заполнения
                ShowStep(1);
            }
        }

        private void ShowDateAndDay()
        {
            // Скрываем все шаги анкеты
            Step1.Visibility = Visibility.Collapsed;
            Step2.Visibility = Visibility.Collapsed;
            Step3.Visibility = Visibility.Collapsed;
            Step4.Visibility = Visibility.Collapsed;
            Step5.Visibility = Visibility.Collapsed;

            // Отображаем текст с текущей датой и днём недели
            DateTextBlock.Text = $"Сегодня: {DateTime.Now:dd.MM.yyyy}, {DateTime.Now:dddd}";
            DateTextBlock.Visibility = Visibility.Visible;
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                int nextStep = int.Parse(tag);
                ShowStep(nextStep);
            }
        }

        private void PreviousStep_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                int previousStep = int.Parse(tag);
                ShowStep(previousStep);
            }
        }

        private void ShowStep(int stepNumber)
        {
            // Скрываем все шаги
            Step1.Visibility = Visibility.Collapsed;
            Step2.Visibility = Visibility.Collapsed;
            Step3.Visibility = Visibility.Collapsed;
            Step4.Visibility = Visibility.Collapsed;
            Step5.Visibility = Visibility.Collapsed;

            // Отображаем текущий шаг
            switch (stepNumber)
            {
                case 1:
                    Step1.Visibility = Visibility.Visible;
                    break;
                case 2:
                    Step2.Visibility = Visibility.Visible;
                    break;
                case 3:
                    Step3.Visibility = Visibility.Visible;
                    break;
                case 4:
                    Step4.Visibility = Visibility.Visible;
                    break;
                case 5:
                    Step5.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SaveUserInfo(GenderInput.Text, AgeInput.Text, HeightInput.Text, WeightInput.Text, ActivityLevelInput.Text))
            {
                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.Navigate(new ProfilePage(new ProfileViewModel()));
            }
            else
            {
                ErrorTextBlock.Text = "Ошибка при сохранении данных. Проверьте введенные значения.";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }
    }
}
