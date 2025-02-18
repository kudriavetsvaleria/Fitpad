using Fitpad.Services;
using Fitpad.ViewModel.PagesViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.IO;
using System.Windows.Media;
using Fitpad.View.Components;

namespace Fitpad.View.Pages
{
    public partial class ProfilePage : Page
    {
        private static ProfilePage _instance;
        private static readonly object _lock = new object();
        private readonly ProfileViewModel _profileViewModel;
        private readonly FirestoreService _firestoreService;
        private bool _isEditing = false;

        public ProfilePage(ProfileViewModel profileViewModel)
        {
            InitializeComponent();
            _firestoreService = new FirestoreService();
            _profileViewModel = profileViewModel;
            DataContext = profileViewModel;

            if (_profileViewModel.CurrentUser != null)
            {
                Console.WriteLine($"🔹 Загружаем данные анкеты для пользователя: {_profileViewModel.CurrentUser.Id}");
                _ = LoadUserInfoAsync(_profileViewModel.CurrentUser.Id);
            }
            else
            {
                Console.WriteLine("❌ Нет текущего пользователя!");
            }

            CheckUserInfoAndShowForm(); // Проверяем данные и при необходимости открываем анкету
        }



        public static ProfilePage GetInstance(ProfileViewModel profileViewModel = null)
        {
            lock (_lock)
            {
                if (_instance == null || profileViewModel != null)
                {
                    _instance = new ProfilePage(profileViewModel ?? new ProfileViewModel());
                    _instance.DataContext = _instance._profileViewModel; // Добавляем обновление контекста
                }

                if (_instance._profileViewModel.CurrentUser != null)
                {
                    _ = _instance.LoadUserInfoAsync(_instance._profileViewModel.CurrentUser.Id);
                }

                return _instance;
            }
        }


        public void UpdateProfileData(ProfileViewModel profileViewModel)
        {
            if (profileViewModel != null)
            {
                _profileViewModel.CurrentUser = profileViewModel.CurrentUser;
                _profileViewModel.CurrentUserInfo = profileViewModel.CurrentUserInfo;
                DataContext = _profileViewModel; // 🔹 Обновляем DataContext
                Console.WriteLine("🔄 Данные профиля обновлены!");
            }
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = true;
            SetEditingState(_isEditing);
        }

        private async void SaveProfileData()
        {
            if (_profileViewModel.CurrentUser == null || string.IsNullOrWhiteSpace(_profileViewModel.CurrentUser.Id))
            {
                MessageBox.Show("Ошибка: Пользователь не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                await _firestoreService.SaveUserInfoAsync(_profileViewModel.CurrentUserInfo);
                
                _isEditing = false;
                SetEditingState(_isEditing);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_profileViewModel.CurrentUser == null || string.IsNullOrWhiteSpace(_profileViewModel.CurrentUser.Id))
            {
                MessageBox.Show("Ошибка: Пользователь не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Валидация данных перед сохранением
            if (!ValidateInputs(out string gender, out int age, out int height, out double weight, out string activityLevel, out string purpose))
            {
                return; // Прерываем выполнение, если валидация не прошла
            }

            // Присваиваем валидные данные модели
            _profileViewModel.CurrentUserInfo.Gender = gender;
            _profileViewModel.CurrentUserInfo.Age = age;
            _profileViewModel.CurrentUserInfo.Height = height;
            _profileViewModel.CurrentUserInfo.Weight = weight;
            _profileViewModel.CurrentUserInfo.ActivityLevel = activityLevel;
            _profileViewModel.CurrentUserInfo.Purpose = purpose;

            try
            {
                await _firestoreService.SaveUserInfoAsync(_profileViewModel.CurrentUserInfo);
                MessageBox.Show("Дані успішно збережені!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при оновленні даних: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            _isEditing = false;
            SetEditingState(_isEditing);
        }


        private void SetEditingState(bool isEditing)
        {
            // Изменяем состояние текстовых полей
            AgeInput.IsReadOnly = !isEditing;
            HeightInput.IsReadOnly = !isEditing;
            WeightInput.IsReadOnly = !isEditing;

            AgeInput.Background = isEditing ? Brushes.White : Brushes.LightGray;
            HeightInput.Background = isEditing ? Brushes.White : Brushes.LightGray;
            WeightInput.Background = isEditing ? Brushes.White : Brushes.LightGray;

            // Переключаем ComboBox и TextBlock без наложения элементов
            GenderTextBlock.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            GenderInput.Opacity = isEditing ? 1 : 0;
            GenderInput.IsHitTestVisible = isEditing;

            ActivityTextBlock.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            ActivityLevelInput.Opacity = isEditing ? 1 : 0;
            ActivityLevelInput.IsHitTestVisible = isEditing;

            PurposeTextBlock.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            PurposeInput.Opacity = isEditing ? 1 : 0;
            PurposeInput.IsHitTestVisible = isEditing;

            // Отображаем нужные кнопки
            EditButton.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            SaveButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            LogoutButton.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible; // Скрываем "Вийти"
        }



        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child is T tChild)
                    {
                        yield return tChild;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void ClearValidationErrors(TextBox textBox)
        {
            var binding = textBox.GetBindingExpression(TextBox.TextProperty);
            if (binding != null)
            {
                Validation.ClearInvalid(binding);
            }
        }


        private bool ValidateInputs(out string gender, out int age, out int height, out double weight, out string activityLevel, out string purpose)
        {
            // Устанавливаем значения по умолчанию
            gender = "";
            age = 0;
            height = 0;
            weight = 0;
            activityLevel = "";
            purpose = "";

            // Получаем данные из UI
            gender = GenderInput.Text;
            activityLevel = ActivityLevelInput.Text;
            purpose = PurposeInput.Text;

            string ageText = AgeInput?.Text;
            string heightText = HeightInput?.Text.Replace(" см", "").Trim();
            string weightText = WeightInput?.Text.Replace(" кг", "").Trim();

            // Проверка на пустые строки
            if (string.IsNullOrWhiteSpace(gender) || string.IsNullOrWhiteSpace(activityLevel) || string.IsNullOrWhiteSpace(purpose))
            {
                MessageBox.Show("Будь ласка, виберіть усі необхідні параметри.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка возраста
            if (!int.TryParse(ageText, out age) || age <= 0 || age > 120)
            {
                MessageBox.Show("Помилка: введіть коректний вік (від 1 до 120 років).", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка роста
            if (!int.TryParse(heightText, out height) || height < 50 || height > 250)
            {
                MessageBox.Show("Помилка: введіть коректний зріст (50 - 250 см).", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка веса
            if (!double.TryParse(weightText, out weight) || weight < 10 || weight > 300)
            {
                MessageBox.Show("Помилка: введіть коректну вагу (10 - 300 кг).", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }


        private async void SaveProfileData_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, изменились ли данные перед сохранением
            bool isChanged = _profileViewModel.CurrentUserInfo.Gender != GenderInput.Text ||
                             _profileViewModel.CurrentUserInfo.Age.ToString() != AgeInput.Text ||
                             _profileViewModel.CurrentUserInfo.Height.ToString() != HeightInput.Text.Replace(" см", "") ||
                             _profileViewModel.CurrentUserInfo.Weight.ToString() != WeightInput.Text.Replace(" кг", "") ||
                             _profileViewModel.CurrentUserInfo.ActivityLevel != ActivityLevelInput.Text ||
                             _profileViewModel.CurrentUserInfo.Purpose != PurposeInput.Text;

            if (!isChanged)
            {
                MessageBox.Show("Дані не змінені!", "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
                _isEditing = false;
                SetEditingState(_isEditing);
                return;
            }

            // Валидация данных перед сохранением
            if (!ValidateInputs(out string gender, out int age, out int height, out double weight, out string activityLevel, out string purpose))
            {
                return; // Прерываем выполнение, если валидация не прошла
            }

            // Присваиваем валидные данные модели
            _profileViewModel.CurrentUserInfo.Gender = gender;
            _profileViewModel.CurrentUserInfo.Age = age;
            _profileViewModel.CurrentUserInfo.Height = height;
            _profileViewModel.CurrentUserInfo.Weight = weight;
            _profileViewModel.CurrentUserInfo.ActivityLevel = activityLevel;
            _profileViewModel.CurrentUserInfo.Purpose = purpose;

            try
            {
                await _firestoreService.SaveUserInfoAsync(_profileViewModel.CurrentUserInfo);
                MessageBox.Show("Дані успішно збережені!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при оновленні даних: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            _isEditing = false;
            SetEditingState(_isEditing);
        }



        private async Task LoadUserInfoAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                Console.WriteLine("❌ Ошибка: UserId пустой!");
                return;
            }

            Console.WriteLine($"🔹 Загружаем данные пользователя с ID: {userId}");

            var firestoreService = new FirestoreService();
            var userInfo = await firestoreService.GetUserInfoAsync(userId);

            if (userInfo != null)
            {
                Console.WriteLine($"✅ Дані анкети успішно завантажені: {userInfo.Gender}, {userInfo.Age}, {userInfo.Height}, {userInfo.Weight}");

                _profileViewModel.CurrentUserInfo = userInfo;
                Dispatcher.Invoke(() => DataContext = _profileViewModel);
            }
            else
            {
                Console.WriteLine("❌ Дані анкети не знайдено.");
            }
        }



        public static void ResetInstance()
        {
            _instance = null;
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("🚪 Выход из аккаунта. Очистка UserSession...");

            // ✅ Очищаем данные пользователя
            UserSession.ClearUserData();

            // ✅ Обновляем состояние навигации
            await MainViewModel.Instance.UpdateNavigationStateAsync();

            // ✅ Перенаправляем пользователя на страницу входа
            NavigationService.Navigate(AccountLoginPage.GetInstance(new ProfileViewModel()));

            Console.WriteLine("✅ Навигация обновлена. Видны только кнопки 'Регистрация', 'Авторизация' и 'Профиль'.");
        }


        private async void CheckUserInfoAndShowForm()
        {
            string userId = UserSession.CurrentUserId;

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("❌ Ошибка: пользователь не найден.");
                return;
            }

            var userInfo = await _firestoreService.GetUserInfoAsync(userId);

            if (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0)
            {
                Console.WriteLine("❌ Данные пользователя отсутствуют. Открываем анкету.");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainViewModel.Instance.CurrentPage = new UserInfoForm(); // ✅ Открываем анкету
                });
            }
        }



        private void ClearCurrentUserFile()
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "current_user.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine("✅ Файл current_user.json успешно удалён.");
                }
                else
                {
                    Console.WriteLine("⚠️ Файл current_user.json уже отсутствует.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при удалении файла: {ex.Message}");
            }
        }

    }
}
