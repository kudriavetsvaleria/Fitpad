using Fitpad.Model.Entities;
using Fitpad.Services;
using Fitpad.View.Components;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Fitpad.View.Pages
{
    public partial class CaloriesPage : Page
    {
        private static CaloriesPage _instance;
        private static UserModel _currentUserCache;
        private readonly FirestoreDb _firestoreDb;

        public CaloriesPage(UserModel currentUser)
        {
            InitializeComponent();
            var firestoreService = new FirestoreService();
            _firestoreDb = firestoreService.GetFirestoreDb();
            _currentUserCache = currentUser;

            Console.WriteLine($"Инициализация CaloriesPage для пользователя: {currentUser.Name}, ID: {currentUser.Id}");
            InitializePageContentAsync();
        }


        public static CaloriesPage GetInstance(UserModel currentUser)
        {
            if (currentUser == null)
            {
                MessageBox.Show("Ошибка: Пользователь не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            if (_instance == null || _currentUserCache == null || _currentUserCache.Id != currentUser.Id)
            {
                _instance = new CaloriesPage(currentUser);
            }
            return _instance;
        }


        private async void InitializePageContentAsync()
        {
            try
            {
                var userInfo = await GetUserInfoAsync(_currentUserCache.Id);

                if (userInfo == null || userInfo.Age == 0 || userInfo.Height == 0 || userInfo.Weight == 0)
                {
                    Console.WriteLine("Данные пользователя не найдены или неполные. Отображение формы ввода.");
                    ShowUserInfoForm();
                }
                else
                {
                    Console.WriteLine("Данные пользователя загружены. Отображение суточной нормы калорий.");
                    ShowCalorieIntake(userInfo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации страницы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task<UserInfoModel> GetUserInfoAsync(string userId) // Изменен тип userId на string
        {
            try
            {
                var userInfoDoc = await _firestoreDb.Collection("UserInfos").Document(userId).GetSnapshotAsync();

                if (userInfoDoc.Exists)
                {
                    return userInfoDoc.ConvertTo<UserInfoModel>();
                }
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void ShowUserInfoForm()
        {
            var userInfoForm = new UserInfoForm(_currentUserCache); // Передаем текущего пользователя
            UserInfoFormContainer.Content = userInfoForm; // Добавляем форму в контейнер
            userInfoForm.Visibility = Visibility.Visible;

            DateTextBlock.Visibility = Visibility.Collapsed;
            CalorieTextBlock.Visibility = Visibility.Collapsed;
        }

        private void ShowCalorieIntake(UserInfoModel userInfo)
        {
            double dailyCalories = CalculateDailyCalorieIntake(userInfo);

            // Получаем детали питания
            var (proteins, fats, carbs, fiber, sugar, salt) = CalculateNutritionDetails(userInfo, dailyCalories);

            // Рассчитываем норму воды (предположим, 30 минут активности)
            double dailyWaterIntake = CalculateWaterIntake(userInfo, activityMinutes: 30);

            // Отображаем калорийность и КБЖУ
            CalorieTextBlock.Text = $"0 / {dailyCalories:0} калорий";
            ProteinTextBlock.Text = $"Белки: 0 / {proteins:0} г";
            FatTextBlock.Text = $"Жиры: 0 / {fats:0} г";
            CarbTextBlock.Text = $"Углеводы: 0 / {carbs:0} г";
            FiberTextBlock.Text = $"Клетчатка: 0 / {fiber:0} г";
            SugarTextBlock.Text = $"Сахар: 0 / {sugar:0} г";
            SaltTextBlock.Text = $"Соль: 0 / {salt:0} г";

            // Отображаем норму воды
            WaterTextBlock.Text = $"Питьевой режим: {dailyWaterIntake:0.0} л";

            // Делаем все элементы видимыми
            CalorieTextBlock.Visibility = Visibility.Visible;
            ProteinTextBlock.Visibility = Visibility.Visible;
            FatTextBlock.Visibility = Visibility.Visible;
            CarbTextBlock.Visibility = Visibility.Visible;
            FiberTextBlock.Visibility = Visibility.Visible;
            SugarTextBlock.Visibility = Visibility.Visible;
            SaltTextBlock.Visibility = Visibility.Visible;
            WaterTextBlock.Visibility = Visibility.Visible;
        }


        private void UpdateMeals(Dictionary<string, List<string>> meals)
        {
            BreakfastTextBlock.Text = meals.ContainsKey("Завтрак") && meals["Завтрак"].Count > 0
                ? string.Join(", ", meals["Завтрак"])
                : "-";

            SecondBreakfastTextBlock.Text = meals.ContainsKey("Второй завтрак") && meals["Второй завтрак"].Count > 0
                ? string.Join(", ", meals["Второй завтрак"])
                : "-";

            LunchTextBlock.Text = meals.ContainsKey("Обед") && meals["Обед"].Count > 0
                ? string.Join(", ", meals["Обед"])
                : "-";

            AfternoonSnackTextBlock.Text = meals.ContainsKey("Полдник") && meals["Полдник"].Count > 0
                ? string.Join(", ", meals["Полдник"])
                : "-";

            DinnerTextBlock.Text = meals.ContainsKey("Ужин") && meals["Ужин"].Count > 0
                ? string.Join(", ", meals["Ужин"])
                : "-";

            SecondDinnerTextBlock.Text = meals.ContainsKey("Второй ужин") && meals["Второй ужин"].Count > 0
                ? string.Join(", ", meals["Второй ужин"])
                : "-";
        }

        private double CalculateWaterIntake(UserInfoModel userInfo, int activityMinutes = 0)
        {
            // Базовый расчет: 30-35 мл воды на 1 кг веса
            double waterIntake = userInfo.Weight * 0.035; // Максимальный коэффициент: 35 мл = 0.035 л

            // Дополнительная вода за активность: 0.5 л за каждые 30 минут
            if (activityMinutes > 0)
            {
                waterIntake += (activityMinutes / 30.0) * 0.5;
            }

            return waterIntake; // Возвращаем норму воды в литрах
        }

        private string GetMealTypeByTime(TimeSpan time)
        {
            if (time >= TimeSpan.FromHours(6) && time < TimeSpan.FromHours(9))
                return "Завтрак";
            if (time >= TimeSpan.FromHours(9) && time < TimeSpan.FromHours(11))
                return "Второй завтрак";
            if (time >= TimeSpan.FromHours(12) && time < TimeSpan.FromHours(14))
                return "Обед";
            if (time >= TimeSpan.FromHours(15) && time < TimeSpan.FromHours(16))
                return "Полдник";
            if (time >= TimeSpan.FromHours(18) && time < TimeSpan.FromHours(20))
                return "Ужин";
            if (time >= TimeSpan.FromHours(20) && time < TimeSpan.FromHours(22))
                return "Второй ужин";

            return "Прочее";
        }


        private double CalculateDailyCalorieIntake(UserInfoModel userInfo)
        {
            double bmr;
            double weight = userInfo.Weight;
            double height = userInfo.Height;
            int age = userInfo.Age;
            string gender = userInfo.Gender;
            string activityLevel = userInfo.ActivityLevel;
            string purpose = userInfo.Purpose;

            // Рассчитываем BMR по формуле Миффлина-Сан Жеора
            if (gender == "Мужской")
            {
                bmr = 88.36 + (13.4 * weight) + (4.8 * height) - (5.7 * age);
            }
            else
            {
                bmr = 447.6 + (9.2 * weight) + (3.1 * height) - (4.3 * age);
            }

            // Множитель активности
            double activityMultiplier = activityLevel switch
            {
                "Низкая" => 1.2,
                "Средняя" => 1.55,
                "Высокая" => 1.9,
                _ => 1.2
            };

            // Рассчитываем TDEE (общее количество калорий)
            double tdee = bmr * activityMultiplier;

            // Корректируем TDEE в зависимости от цели
            tdee = purpose switch
            {
                "Похудение" => tdee - 400, // Уменьшаем TDEE на 400 калорий (можно изменить диапазон)
                "Набор массы" => tdee + 400, // Увеличиваем TDEE на 400 калорий (можно изменить диапазон)
                _ => tdee // Для "Сохранение массы" оставляем без изменений
            };

            return tdee;
        }

        private (double Proteins, double Fats, double Carbs, double Fiber, double Sugar, double Salt) CalculateNutritionDetails(UserInfoModel userInfo, double dailyCalories)
        {
            // Соотношение макронутриентов в зависимости от цели
            (double ProteinPercent, double FatPercent, double CarbPercent) = userInfo.Purpose switch
            {
                "Похудение" => (0.25, 0.225, 0.475), // Белки 25%, Жиры 22.5%, Углеводы 47.5%
                "Набор массы" => (0.175, 0.275, 0.55), // Белки 17.5%, Жиры 27.5%, Углеводы 55%
                _ => (0.175, 0.275, 0.55) // По умолчанию поддержание веса: Белки 17.5%, Жиры 27.5%, Углеводы 55%
            };

            // Расчет КБЖУ
            double proteins = (dailyCalories * ProteinPercent) / 4; // Белки (калории на грамм: 4)
            double fats = (dailyCalories * FatPercent) / 9; // Жиры (калории на грамм: 9)
            double carbs = (dailyCalories * CarbPercent) / 4; // Углеводы (калории на грамм: 4)

            // Расчет клетчатки
            double fiber = (userInfo.Gender == "Мужской" ?
                           (userInfo.Age <= 50 ? 35 : 27) :
                           (userInfo.Age <= 50 ? 23 : 20));

            // Расчет сахара (не более 10% калорий, в идеале 5%)
            double sugar = dailyCalories * 0.05 / 4; // Сахар (калории на грамм: 4)

            // Суточная норма соли: 5 г
            double salt = 5;

            return (proteins, fats, carbs, fiber, sugar, salt);
        }




    }
}
