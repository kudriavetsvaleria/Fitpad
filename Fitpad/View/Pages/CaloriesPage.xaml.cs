using Fitpad.Model.Entities;
using Fitpad.Services;
using Fitpad.View.Components;
using Google.Cloud.Firestore;
using System;
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

            InitializePageContentAsync();
        }


        public static CaloriesPage GetInstance(UserModel currentUser)
        {
            if (_instance == null || _currentUserCache == null || _currentUserCache.Id != currentUser.Id)
            {
                _instance = new CaloriesPage(currentUser);
            }
            return _instance;
        }

        private async void InitializePageContentAsync()
        {
            var userInfo = await GetUserInfoAsync(_currentUserCache.Id);

            if (userInfo == null || userInfo.Age == 0 || userInfo.Height == 0 || userInfo.Weight == 0)
            {
                ShowUserInfoForm();
            }
            else
            {
                ShowCalorieIntake(userInfo);
            }
        }

        private async Task<UserInfoModel> GetUserInfoAsync(int userId)
        {
            try
            {
                var userInfoDoc = await _firestoreDb.Collection("UserInfos").Document(userId.ToString()).GetSnapshotAsync();

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
            UserInfoForm.Visibility = Visibility.Visible;
            DateTextBlock.Visibility = Visibility.Collapsed;
            CalorieTextBlock.Visibility = Visibility.Collapsed;
        }

        private void ShowCalorieIntake(UserInfoModel userInfo)
        {
            double dailyCalories = CalculateDailyCalorieIntake(userInfo);
            CalorieTextBlock.Text = $"0 / {dailyCalories:0} калорий";
            CalorieTextBlock.Visibility = Visibility.Visible;
        }

        private double CalculateDailyCalorieIntake(UserInfoModel userInfo)
        {
            double bmr;
            double weight = userInfo.Weight;
            double height = userInfo.Height;
            int age = userInfo.Age;
            string gender = userInfo.Gender;
            string activityLevel = userInfo.ActivityLevel;

            if (gender == "Мужской")
            {
                bmr = 88.36 + (13.4 * weight) + (4.8 * height) - (5.7 * age);
            }
            else
            {
                bmr = 447.6 + (9.2 * weight) + (3.1 * height) - (4.3 * age);
            }

            double activityMultiplier = activityLevel switch
            {
                "Низкая" => 1.2,
                "Средняя" => 1.55,
                "Высокая" => 1.9,
                _ => 1.2
            };

            return bmr * activityMultiplier;
        }
    }
}
