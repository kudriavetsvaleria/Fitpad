// MainViewModel.cs
using Fitpad.View.Pages;
using Fitpad.ViewModel.PagesViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using Fitpad.Model;
using System.Windows;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<Type, Page> _pageCache = new Dictionary<Type, Page>();
    private readonly ProfileViewModel _profileViewModel = new ProfileViewModel();
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

        ShowNewsCommand = new RelayCommand(o => NavigateTo<NewsPage>());
        ShowFavoritesCommand = new RelayCommand(o => NavigateTo<FavoritesPage>());
        ShowNutritionCommand = new RelayCommand(o => NavigateTo<NutritionPage>());
        ShowWorkoutsCommand = new RelayCommand(o => NavigateTo<WorkoutsPage>());
        ShowProfileCommand = new RelayCommand(o => NavigateToProfilePage());
        ShowAccountLoginCommand = new RelayCommand(o => NavigateTo<AccountLoginPage>());
        ShowAccountRegistrationCommand = new RelayCommand(o => NavigateTo<AccountRegistrationPage>());
        ShowCaloriesCommand = new RelayCommand(o => NavigateTo<CaloriesPage>());

        ToggleNavigationCommand = new RelayCommand(o => IsNavigationExpanded = !IsNavigationExpanded);

        // Проверяем, выполнен ли вход пользователя
        var storedUser = UserStorage.Load();
        if (storedUser != null)
        {
            CurrentPage = GetPageInstance<NewsPage>();
        }
        else
        {
            CurrentPage = GetPageInstance<AccountLoginPage>();
        }
    }

    public void NavigateTo<T>() where T : Page
    {
        var currentUser = UserStorage.GetCurrentUser();

        // Проверяем, авторизован ли пользователь
        if (typeof(T) == typeof(CaloriesPage) && currentUser == null)
        {
            MessageBox.Show("Войдите в аккаунт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return; // Прерываем выполнение метода, остаёмся на текущей странице
        }

        var page = GetPageInstance<T>();

        if (CurrentPage is Page currentPage && currentPage.NavigationService != null)
        {
            currentPage.NavigationService.Navigate(page);
        }
        else
        {
            CurrentPage = page;
        }
    }


    public void NavigateToProfilePage()
    {
        var storedUser = UserStorage.Load(); // Загружаем данные пользователя из хранилища
        if (storedUser != null)
        {
            var profileViewModel = new ProfileViewModel(storedUser); // Создаём новый экземпляр ViewModel с данными
            CurrentPage = ProfilePage.GetInstance(profileViewModel); // Передаём ViewModel в GetInstance
        }
    }


    private Page GetPageInstance<T>() where T : Page
    {
        var type = typeof(T);

        // Проверяем, если это CaloriesPage, создаем новый экземпляр при смене пользователя
        if (type == typeof(CaloriesPage))
        {
            var currentUser = UserStorage.GetCurrentUser(); // Получаем текущего пользователя
            return CaloriesPage.GetInstance(currentUser);   // Всегда создаем актуальный экземпляр страницы
        }

        if (!_pageCache.TryGetValue(type, out var page))
        {
            if (type == typeof(ProfilePage))
            {
                page = ProfilePage.GetInstance(_profileViewModel);
            }
            else if (type == typeof(AccountLoginPage))
            {
                page = new AccountLoginPage(_profileViewModel);
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
