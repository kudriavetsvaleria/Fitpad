using Fitpad.View.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<Type, Page> _pageCache = new Dictionary<Type, Page>();

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

    public MainViewModel()
    {
        ShowNewsCommand = new RelayCommand(o => NavigateTo<NewsPage>());
        ShowFavoritesCommand = new RelayCommand(o => NavigateTo<FavoritesPage>());
        ShowNutritionCommand = new RelayCommand(o => NavigateTo<NutritionPage>());
        ShowWorkoutsCommand = new RelayCommand(o => NavigateTo<WorkoutsPage>());
        ShowProfileCommand = new RelayCommand(o => NavigateTo<ProfilePage>());
        ShowAccountLoginCommand = new RelayCommand(o => NavigateTo<AccountLoginPage>());
        ShowAccountRegistrationCommand = new RelayCommand(o => NavigateTo<AccountRegistrationPage>());
        ShowCaloriesCommand = new RelayCommand(o => NavigateTo<CaloriesPage>());

        // Открываем страницу новостей по умолчанию
        CurrentPage = GetPageInstance<NewsPage>();
        ToggleNavigationCommand = new RelayCommand(o => IsNavigationExpanded = !IsNavigationExpanded);
    }

    public void NavigateTo<T>() where T : Page, new()
    {
        var page = GetPageInstance<T>();

        if (CurrentPage is Page currentPage && currentPage.NavigationService != null)
        {
            currentPage.NavigationService.Navigate(page);
        }
        else
        {
            // Логика для обработки случаев без NavigationService
            CurrentPage = page;
        }
    }



    private Page GetPageInstance<T>() where T : Page, new()
    {
        var type = typeof(T);

        if (!_pageCache.TryGetValue(type, out var page))
        {
            page = new T();
            _pageCache[type] = page;
        }

        return page;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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

}
