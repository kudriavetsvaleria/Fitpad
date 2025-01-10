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

public class MainViewModel : INotifyPropertyChanged
{
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

    public ICommand ShowNewsCommand { get; }
    public ICommand ShowFavoritesCommand { get; }
    public ICommand ShowNutritionCommand { get; }
    public ICommand ShowWorkoutsCommand { get; }
    public ICommand ShowProfileCommand { get; }
    public ICommand ShowAccountLoginCommand { get; }
    public ICommand ShowAccountRegistrationCommand { get; }
    public ICommand ToggleNavigationCommand { get; }
    public ICommand ShowCaloriesCommand { get; }

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
        ShowFavoritesCommand = new RelayCommand(async o => await NavigateToAsync<FavoritesPage>());
        ShowNutritionCommand = new RelayCommand(async o => await NavigateToAsync<NutritionPage>());
        ShowWorkoutsCommand = new RelayCommand(async o => await NavigateToAsync<WorkoutsPage>());
        ShowProfileCommand = new RelayCommand(async o => await NavigateToProfilePageAsync());
        ShowAccountLoginCommand = new RelayCommand(async o => await NavigateToAsync<AccountLoginPage>());
        ShowAccountRegistrationCommand = new RelayCommand(async o => await NavigateToAsync<AccountRegistrationPage>());
        ShowCaloriesCommand = new RelayCommand(async o => await NavigateToAsync<CaloriesPage>());

        ToggleNavigationCommand = new RelayCommand(o => IsNavigationExpanded = !IsNavigationExpanded);

        // Проверяем, выполнен ли вход пользователя
        InitializeCurrentPageAsync();
    }

    private async void InitializeCurrentPageAsync()
    {
        var storedUser = LoadCurrentUserFromFile();

        if (storedUser != null)
        {
            Console.WriteLine("Найден авторизованный пользователь. Выполняется автоматический вход...");
            CurrentPage = await GetPageInstanceAsync<NewsPage>();
        }
        else
        {
            storedUser = await _userRepository.GetCurrentUserAsync();
            if (storedUser != null)
            {
                CurrentPage = await GetPageInstanceAsync<NewsPage>();
            }
            else
            {
                CurrentPage = await GetPageInstanceAsync<AccountLoginPage>();
            }
        }
    }


    // Метод для загрузки данных пользователя из JSON-файла
    private UserModel LoadCurrentUserFromFile()
    {
        try
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current_user.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<dynamic>(json);

                // Восстанавливаем ID текущего пользователя
                UserRepository.CurrentUserId = data.UserId;

                // Восстанавливаем данные пользователя
                return JsonConvert.DeserializeObject<UserModel>(Convert.ToString(data.User));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке данных пользователя: {ex.Message}");
        }
        return null;
    }


    public async Task NavigateToAsync<T>() where T : Page
    {
        var currentUser = await _userRepository.GetCurrentUserAsync();

        // Проверяем, авторизован ли пользователь
        if (typeof(T) == typeof(CaloriesPage) && currentUser == null)
        {
            MessageBox.Show("Войдите в аккаунт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return; // Прерываем выполнение метода, остаёмся на текущей странице
        }

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

        if (type == typeof(CaloriesPage))
        {
            var currentUser = await _userRepository.GetCurrentUserAsync();
            if (currentUser == null)
            {
                MessageBox.Show("Ошибка: Пользователь не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            return CaloriesPage.GetInstance(currentUser);
        }

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
                page = (Page)Activator.CreateInstance(type); // Создание страницы без параметров
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
