using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Fitpad.Model;
using Fitpad.View.Components;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad.View.Pages
{
    public partial class CaloriesPage : Page
    {
        private static CaloriesPage _instance; // Статическое поле для хранения экземпляра
        private static UserModel _currentUserCache; // Кэш текущего пользователя

        private readonly UserInfoViewModel _viewModel;

        // Публичный конструктор без параметров
        public CaloriesPage() : this(UserStorage.GetCurrentUser())
        {
        }

        public CaloriesPage(UserModel currentUser)
        {
            if (currentUser == null)
            {
                MessageBox.Show("Войдите в аккаунт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

                if (NavigationService != null)
                {
                    // Если NavigationService доступен, используем его для навигации
                    NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));
                }
                else
                {
                    // Если NavigationService недоступен, используем NavigateTo из MainViewModel
                    MainViewModel.Instance.NavigateTo<AccountLoginPage>();
                }

                return; // Прерываем выполнение конструктора
            }

            InitializeComponent();
            _viewModel = new UserInfoViewModel(currentUser);
            DataContext = _viewModel;

            // Проверяем наличие данных анкеты
            if (HasExistingUserInfo(currentUser.Id))
            {
                // Данные анкеты существуют, показываем норму калорий и дату
                ShowDateAndDay();
                ShowCalorieIntake();
            }
            else
            {
                // Данных анкеты нет, показываем форму для заполнения
                ShowUserInfoForm();
            }
        }



        public static CaloriesPage GetInstance(UserModel currentUser)
        {
            // Проверяем, если пользователь изменился, создаем новый экземпляр страницы
            if (_instance == null || _currentUserCache == null || _currentUserCache.Id != currentUser.Id)
            {
                _instance = new CaloriesPage(currentUser);
                _currentUserCache = currentUser;
            }

            return _instance;
        }

        private bool HasExistingUserInfo(int userId)
        {
            using (var context = new ApplicationDbContext())
            {
                // Проверяем наличие записи в таблице UserInfos для текущего пользователя
                return context.UserInfos.Any(info => info.UserId == userId);
            }
        }

        private void ShowDateAndDay()
        {
            // Отображаем текст с текущей датой и днём недели
            DateTextBlock.Text = $"Сегодня: {DateTime.Now:dd.MM.yyyy}, {DateTime.Now:dddd}";
            DateTextBlock.Visibility = Visibility.Visible;
        }

        private void ShowCalorieIntake()
        {
            double dailyCalories = CalculateDailyCalorieIntake();
            CalorieTextBlock.Text = $"0 / {dailyCalories:0} калорий";
            CalorieTextBlock.Visibility = Visibility.Visible;
        }

        private double CalculateDailyCalorieIntake()
        {
            double bmr;
            double weight = _viewModel.CurrentUserInfo.Weight;
            double height = _viewModel.CurrentUserInfo.Height;
            int age = _viewModel.CurrentUserInfo.Age;
            string gender = _viewModel.CurrentUserInfo.Gender;
            string activityLevel = _viewModel.CurrentUserInfo.ActivityLevel;

            // Формула Харриса-Бенедикта для мужчин и женщин
            if (gender == "Мужской")
            {
                bmr = 88.36 + (13.4 * weight) + (4.8 * height) - (5.7 * age);
            }
            else
            {
                bmr = 447.6 + (9.2 * weight) + (3.1 * height) - (4.3 * age);
            }

            // Коэффициент активности
            double activityMultiplier = activityLevel switch
            {
                "Низкая" => 1.2,
                "Средняя" => 1.55,
                "Высокая" => 1.9,
                _ => 1.2 // По умолчанию низкая активность
            };

            return bmr * activityMultiplier;
        }

        private void ShowUserInfoForm()
        {
            // Отображаем компонент формы анкеты
            UserInfoForm.Visibility = Visibility.Visible;
            DateTextBlock.Visibility = Visibility.Collapsed; // Скрываем дату
            CalorieTextBlock.Visibility = Visibility.Collapsed; // Скрываем норму калорий
        }

        public static void ResetInstance()
        {
            _instance = null; // Сбрасываем экземпляр страницы
            _currentUserCache = null; // Сбрасываем кэш пользователя
        }
    }
}
