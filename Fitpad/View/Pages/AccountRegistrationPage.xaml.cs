using Fitpad.ViewModel.PagesViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Fitpad.View.Pages
{
    public partial class AccountRegistrationPage : Page
    {
        private static AccountRegistrationPage _instance;
        public AccountRegistrationPage()
        {
            InitializeComponent();
        }

        public static AccountRegistrationPage GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AccountRegistrationPage();
            }
            return _instance;
        }

        // Обработчик для кнопки "Далее" на первом шаге
        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int nextStep = int.Parse(button.Tag.ToString());
            ShowStep(nextStep);
        }

        // Обработчик для кнопки "Назад" на шагах 2 и 3
        private void PreviousStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int previousStep = int.Parse(button.Tag.ToString());
            ShowStep(previousStep);
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика регистрации пользователя
            MessageBox.Show("Регистрация завершена!");
        }

        private void NavigateToLoginPage_Click(object sender, RoutedEventArgs e)
        {
            // Логика перехода на страницу авторизации
            //NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
        }

        // Обработчик для кнопки "Тест"
        private void TestStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int nextStep = int.Parse(button.Tag.ToString());
            ShowStep(nextStep);
        }

        // Обработчик для кнопки "ОК" после успешной регистрации
        private void SuccessOkButton_Click(object sender, RoutedEventArgs e)
        {
            SuccessMessageOverlay.Visibility = Visibility.Collapsed;

            // Передаем новый экземпляр ProfileViewModel при вызове GetInstance
            NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
        }

/*        private void NavigateToLoginPage_Click(object sender, RoutedEventArgs e)
        {
            // Передаем новый экземпляр ProfileViewModel при вызове GetInstance
            NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
        }*/


        // Переход между шагами регистрации
        private void ShowStep(int step)
        {
            Step1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
