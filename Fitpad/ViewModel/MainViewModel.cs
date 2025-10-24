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
    public bool IsFullNavigationVisible => IsUserAuthenticated && IsDashboardComplete;
    public bool IsLimitedNavigationVisible => !IsFullNavigationVisible;
    private readonly Dictionary<Type, Page> _pageCache = new Dictionary<Type, Page>();
    private readonly DashboardViewModel _DashboardViewModel;
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

    private bool _isDashboardComplete;
    public bool IsDashboardComplete
    {
        get => _isDashboardComplete;
        set
        {
            _isDashboardComplete = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFullNavigationVisible));
        }
    }

    public ICommand ShowNewsCommand { get; }
    public ICommand ShowNutritionCommand { get; }
    public ICommand ShowWorkoutsCommand { get; }
    public ICommand ShowDashboardCommand { get; }
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
        _DashboardViewModel = new DashboardViewModel();

        ShowNewsCommand = new RelayCommand(async o => await NavigateToAsync<NewsPage>());
        ShowNutritionCommand = new RelayCommand(async o => await NavigateToAsync<NutritionPage>());
        ShowDashboardCommand = new RelayCommand(async o => await NavigateToDashboardPageAsync());
        ShowAccountLoginCommand = new RelayCommand(async o => await NavigateToAsync<AccountLoginPage>());
        ShowAccountRegistrationCommand = new RelayCommand(async o => await NavigateToAsync<AccountRegistrationPage>());

        ShowDishesCommand = new RelayCommand(async o => await NavigateToAsync<DishesPage>());
        ShowCalculateNutritionCommand = new RelayCommand(async o => await NavigateToAsync<CalculateNutritionPage>());
        ShowConstructorPageCommand = new RelayCommand(async o => await NavigateToAsync<ConstructorPage>());

        ToggleNavigationCommand = new RelayCommand(o => IsNavigationExpanded = !IsNavigationExpanded);

        // Добавляем команду выхода
        LogoutCommand = new RelayCommand(o => Logout());

        InitializeCurrentPageAsync();

        InitializeNavigationState();
    }

    private async void InitializeNavigationState()
    {
        await UpdateNavigationStateAsync();
    }

    // В MainViewModel
    public void Logout()   // было private void Logout()
    {
        Console.WriteLine("🚪 Выход из аккаунта...");

        // 1) чистим сессию
        UserSession.Logout();

        // 2) сбрасываем состояние
        IsUserAuthenticated = false;
        IsDashboardComplete = false;

        // 3) (опционально) очисти кэш страниц, чтобы не держать старые статики
        _pageCache.Clear();
        DashboardPage.ResetInstance();

        // 4) обновляем UI
        OnPropertyChanged(nameof(IsUserAuthenticated));
        OnPropertyChanged(nameof(IsDashboardComplete));
        OnPropertyChanged(nameof(IsFullNavigationVisible));
        OnPropertyChanged(nameof(IsLimitedNavigationVisible));

        // 5) ЕДИНСТВЕННОЕ место, где переключаемся на логин
        CurrentPage = AccountLoginPage.GetInstance(new DashboardViewModel());

        Console.WriteLine("Выход выполнен. Показаны 'Регистрация', 'Авторизация', 'Профіль'.");
    }



    private async void InitializeCurrentPageAsync()
    {
        Console.WriteLine("🏁 Инициализация стартовой страницы...");

        UserSession.LoadUserIdFromFile();
        Console.WriteLine($"🔹 UserSession.CurrentUserId = {UserSession.CurrentUserId}");

        if (string.IsNullOrEmpty(UserSession.CurrentUserId))
        {
            Console.WriteLine("Пользователь не авторизован. Показываем ограниченное меню.");
            IsUserAuthenticated = false;
            IsDashboardComplete = false;
            CurrentPage = AccountRegistrationPage.GetInstance();
            return;
        }

        var firestoreService = new FirestoreService();
        var userInfo = await firestoreService.GetUserInfoAsync(UserSession.CurrentUserId);

        if (userInfo != null && userInfo.Weight > 0 && userInfo.Height > 0 && userInfo.Age > 0)
        {
            Console.WriteLine("Профиль заполнен. Полное меню доступно.");
            IsUserAuthenticated = true;
            IsDashboardComplete = true;
            CurrentPage = NewsPage.GetInstance();
        }
        else
        {
            Console.WriteLine("⚠ Пользователь авторизован, но профиль не заполнен. Ограниченное меню.");
            IsUserAuthenticated = true;
            IsDashboardComplete = false;
            CurrentPage = DashboardPage.GetInstance(new DashboardViewModel(new UserModel { Id = UserSession.CurrentUserId }));
        }

        var fs = new FirestoreService();
        await fs.BackfillDaySummariesFromRegistrationAsync(UserSession.CurrentUserId);
    }

    public async Task UpdateNavigationStateAsync()
    {
        // ... твоя текущая логика ...
        var firestoreService = new FirestoreService();
        var userInfo = await firestoreService.GetUserInfoAsync(UserSession.CurrentUserId);

        if (userInfo != null && userInfo.Weight > 0 && userInfo.Height > 0 && userInfo.Age > 0)
        {
            IsUserAuthenticated = true;
            IsDashboardComplete = true;
        }
        else
        {
            IsUserAuthenticated = true;
            IsDashboardComplete = false;
        }

        OnPropertyChanged(nameof(IsUserAuthenticated));
        OnPropertyChanged(nameof(IsDashboardComplete));
        OnPropertyChanged(nameof(IsFullNavigationVisible));
        OnPropertyChanged(nameof(IsLimitedNavigationVisible));
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

        if (userInfo == null || string.IsNullOrEmpty(userInfo.Gender) ||
            userInfo.Age == 0 || userInfo.Height == 0 || userInfo.Weight == 0)
        {
            Console.WriteLine("❌ Данные пользователя отсутствуют. Открываем форму UserInfoWindow...");

            // ✅ Исправленный вызов
            var userInfoForm = new UserInfoWindow(_DashboardViewModel);
            userInfoForm.Owner = Application.Current.MainWindow;
            userInfoForm.ShowDialog();

            // После закрытия окна
            await UpdateNavigationStateAsync();
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


    public async Task NavigateToDashboardPageAsync()
    {
        var storedUser = await _userRepository.GetCurrentUserAsync();
        if (storedUser != null)
        {
            var DashboardViewModel = new DashboardViewModel(storedUser);
            CurrentPage = DashboardPage.GetInstance(DashboardViewModel);
        }
    }

    private async Task<Page> GetPageInstanceAsync<T>() where T : Page
    {
        var type = typeof(T);

        if (!_pageCache.TryGetValue(type, out var page))
        {
            if (type == typeof(DashboardPage))
            {
                var currentUser = await _userRepository.GetCurrentUserAsync();
                var DashboardViewModel = new DashboardViewModel(currentUser);
                page = DashboardPage.GetInstance(DashboardViewModel);
            }
            else if (type == typeof(AccountLoginPage))
            {
                page = AccountLoginPage.GetInstance(new DashboardViewModel());
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
