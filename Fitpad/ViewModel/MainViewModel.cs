using Fitpad.View.Pages;
using Fitpad.ViewModel.PagesViewModels;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using System.IO;
using Fitpad.Services;
using Fitpad.View.Components;

public class MainViewModel : INotifyPropertyChanged
{
    public bool IsFullNavigationVisible => IsUserAuthenticated && IsProfileComplete;
    public bool IsLimitedNavigationVisible => !IsFullNavigationVisible;
    private readonly Dictionary<Type, Page> _pageCache = new Dictionary<Type, Page>();
    private readonly ProfileViewModel _profileViewModel;
    private readonly UserRepository _userRepository;
    public static MainViewModel Instance { get; private set; }

    private object _currentPage;
    public object CurrentPage
    {
        get => _currentPage;
        set
        {
            _currentPage = value;
            OnPropertyChanged();
        }
    }

    private bool _isUserAuthenticated;
    public bool IsUserAuthenticated
    {
        get => _isUserAuthenticated;
        set
        {
            _isUserAuthenticated = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFullNavigationVisible));
            OnPropertyChanged(nameof(IsLimitedNavigationVisible));
        }
    }

    private bool _isProfileComplete;
    public bool IsProfileComplete
    {
        get => _isProfileComplete;
        set
        {
            _isProfileComplete = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFullNavigationVisible));
        }
    }

    public ICommand ShowNewsCommand { get; }
    public ICommand ShowNutritionCommand { get; }
    public ICommand ShowWorkoutsCommand { get; }
    public ICommand ShowProfileCommand { get; }
    public ICommand ShowAccountLoginCommand { get; }
    public ICommand ShowAccountRegistrationCommand { get; }
    public ICommand ToggleNavigationCommand { get; }

    public ICommand ShowDishesCommand { get; }
    public ICommand ShowCalculateNutritionCommand { get; }
    public ICommand ShowConstructorPageCommand { get; }

    public ICommand LogoutCommand { get; }

    private bool _isNavigationExpanded = true;
    public bool IsNavigationExpanded
    {
        get => _isNavigationExpanded;
        set
        {
            _isNavigationExpanded = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {
        Instance = this;
        _userRepository = new UserRepository();
        _profileViewModel = new ProfileViewModel();

        ShowNewsCommand = new RelayCommand(async o => await NavigateToAsync<NewsPage>());
        ShowNutritionCommand = new RelayCommand(async o => await NavigateToAsync<NutritionPage>());
        ShowProfileCommand = new RelayCommand(async o => await NavigateToProfilePageAsync());
        ShowAccountLoginCommand = new RelayCommand(async o => await NavigateToAsync<AccountLoginPage>());
        ShowAccountRegistrationCommand = new RelayCommand(async o => await NavigateToAsync<AccountRegistrationPage>());

        ShowDishesCommand = new RelayCommand(async o => await NavigateToAsync<DishesPage>());
        ShowCalculateNutritionCommand = new RelayCommand(async o => await NavigateToAsync<CalculateNutritionPage>());
        ShowConstructorPageCommand = new RelayCommand(async o => await NavigateToAsync<ConstructorPage>());

        ToggleNavigationCommand = new RelayCommand(o => IsNavigationExpanded = !IsNavigationExpanded);

        // ✅ Добавляем команду выхода
        LogoutCommand = new RelayCommand(o => Logout());

        InitializeCurrentPageAsync();
    }

    private void Logout()
    {
        Console.WriteLine("🚪 Выход из аккаунта...");

        // ✅ Удаляем сохраненный UserID
        UserSession.Logout();

        // ✅ Сбрасываем состояние пользователя
        IsUserAuthenticated = false;
        IsProfileComplete = false;


        // ✅ Обновляем навигацию
        OnPropertyChanged(nameof(IsUserAuthenticated));
        OnPropertyChanged(nameof(IsProfileComplete));
        OnPropertyChanged(nameof(IsFullNavigationVisible));
        OnPropertyChanged(nameof(IsLimitedNavigationVisible));

        // ✅ Перенаправляем на страницу входа
        CurrentPage = AccountLoginPage.GetInstance(new ProfileViewModel());

        Console.WriteLine("✅ Выход выполнен. Отображаются только кнопки 'Регистрация', 'Авторизация' и 'Профиль'.");
    }


    private async void InitializeCurrentPageAsync()
    {
        Console.WriteLine("🏁 Инициализация стартовой страницы...");

        UserSession.LoadUserIdFromFile();
        Console.WriteLine($"🔹 UserSession.CurrentUserId = {UserSession.CurrentUserId}");

        if (string.IsNullOrEmpty(UserSession.CurrentUserId))
        {
            Console.WriteLine("❌ Пользователь не авторизован. Показываем ограниченное меню.");
            IsUserAuthenticated = false;
            IsProfileComplete = false;
            CurrentPage = AccountRegistrationPage.GetInstance();
            return;
        }

        var firestoreService = new FirestoreService();
        var userInfo = await firestoreService.GetUserInfoAsync(UserSession.CurrentUserId);

        if (userInfo != null && userInfo.Weight > 0 && userInfo.Height > 0 && userInfo.Age > 0)
        {
            Console.WriteLine("✅ Профиль заполнен. Полное меню доступно.");
            IsUserAuthenticated = true;
            IsProfileComplete = true;
            CurrentPage = NewsPage.GetInstance();
        }
        else
        {
            Console.WriteLine("⚠ Пользователь авторизован, но профиль не заполнен. Ограниченное меню.");
            IsUserAuthenticated = true;
            IsProfileComplete = false;
            CurrentPage = ProfilePage.GetInstance(new ProfileViewModel(new UserModel { Id = UserSession.CurrentUserId }));
        }
    }

    public async Task UpdateNavigationStateAsync()
    {
        Console.WriteLine("🔄 Обновление состояния навигации...");

        if (string.IsNullOrEmpty(UserSession.CurrentUserId))
        {
            IsUserAuthenticated = false;
            IsProfileComplete = false;
            Console.WriteLine("❌ Пользователь не авторизован.");
            return;
        }

        var firestoreService = new FirestoreService();
        var userInfo = await firestoreService.GetUserInfoAsync(UserSession.CurrentUserId);

        if (userInfo != null && userInfo.Weight > 0 && userInfo.Height > 0 && userInfo.Age > 0)
        {
            IsUserAuthenticated = true;
            IsProfileComplete = true;
            Console.WriteLine("✅ Профиль заполнен. Все кнопки доступны.");
        }
        else
        {
            IsUserAuthenticated = true;
            IsProfileComplete = false;
            Console.WriteLine("⚠ Пользователь авторизован, но профиль не заполнен.");
        }

        // Обновляем интерфейс
        OnPropertyChanged(nameof(IsFullNavigationVisible));
        OnPropertyChanged(nameof(IsLimitedNavigationVisible));
    }



    // Метод для загрузки данных пользователя из JSON-файла
    private UserModel LoadCurrentUserFromFile()
    {
        try
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fitpad", "current_user.json");
            Console.WriteLine($"📂 Ищем файл по пути: {filePath}");

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Console.WriteLine($"📜 Загруженные данные: {json}");

                var data = JsonConvert.DeserializeObject<dynamic>(json);

                if (data != null && data.UserId != null)
                {
                    UserRepository.CurrentUserId = data.UserId.ToString();
                    Console.WriteLine($"✅ UserID загружен: {UserRepository.CurrentUserId}");

                    return JsonConvert.DeserializeObject<UserModel>(JsonConvert.SerializeObject(data.User));
                }
                else
                {
                    Console.WriteLine("❌ Ошибка: данные в файле некорректны!");
                }
            }
            else
            {
                Console.WriteLine("❌ Файл `current_user.json` не найден!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка загрузки данных пользователя: {ex.Message}");
        }
        return null;
    }


    public async Task NavigateToAsync<T>() where T : Page
    {
        var page = await GetPageInstanceAsync<T>();

        if (CurrentPage is Page currentPage && currentPage.NavigationService != null)
        {
            currentPage.NavigationService.Navigate(page);
        }
        else
        {
            CurrentPage = page;
        }
    }

    private async Task OpenCalculatorAsync()
    {
        var firestoreService = new FirestoreService();
        var userInfo = await firestoreService.GetUserInfoAsync(UserSession.CurrentUserId);

        // Проверяем, есть ли данные пользователя
        if (userInfo == null || string.IsNullOrEmpty(userInfo.Gender) ||
            userInfo.Age == 0 || userInfo.Height == 0 || userInfo.Weight == 0)
        {
            Console.WriteLine("❌ Данные пользователя отсутствуют. Открываем форму UserInfoForm...");

            // Создаём форму для заполнения данных
            var userInfoForm = new UserInfoForm();
            var window = new Window
            {
                Title = "Заповніть особисті дані",
                Content = userInfoForm,
                Width = 350,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            // Открываем окно в модальном режиме
            bool? result = window.ShowDialog();

            // Если данные успешно сохранены, открыть калькулятор
            if (result == true)
            {
                Console.WriteLine("✅ Данные успешно сохранены. Открываем калькулятор...");
                OpenCalculator();
            }
            else
            {
                Console.WriteLine("❌ Пользователь закрыл форму, калькулятор не открываем.");
            }
        }
        else
        {
            Console.WriteLine("✅ Данные пользователя корректны. Открываем калькулятор...");
            OpenCalculator();
        }
    }


    private void OpenCalculator()
    {
        CurrentPage = new CalculateNutritionPage();
    }


    public async Task NavigateToProfilePageAsync()
    {
        var storedUser = await _userRepository.GetCurrentUserAsync();
        if (storedUser != null)
        {
            var profileViewModel = new ProfileViewModel(storedUser);
            CurrentPage = ProfilePage.GetInstance(profileViewModel);
        }
    }

    private async Task<Page> GetPageInstanceAsync<T>() where T : Page
    {
        var type = typeof(T);

        if (!_pageCache.TryGetValue(type, out var page))
        {
            if (type == typeof(ProfilePage))
            {
                var currentUser = await _userRepository.GetCurrentUserAsync();
                var profileViewModel = new ProfileViewModel(currentUser);
                page = ProfilePage.GetInstance(profileViewModel);
            }
            else if (type == typeof(AccountLoginPage))
            {
                page = AccountLoginPage.GetInstance(new ProfileViewModel());
            }
            else
            {
                page = (Page)Activator.CreateInstance(type);
            }

            _pageCache[type] = page;
        }

        return page;
    }



    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
