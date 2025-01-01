using Fitpad.View.Pages;
using Fitpad.ViewModel.PagesViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<Type, Page> _pageCache = new Dictionary<Type, Page>();
    private readonly ProfileViewModel _profileViewModel = new ProfileViewModel();

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
        ShowProfileCommand = new RelayCommand(o => NavigateToProfilePage());
        ShowAccountLoginCommand = new RelayCommand(o => NavigateTo<AccountLoginPage>());
        ShowAccountRegistrationCommand = new RelayCommand(o => NavigateTo<AccountRegistrationPage>());
        ShowCaloriesCommand = new RelayCommand(o => NavigateTo<CaloriesPage>());

        // Открываем страницу новостей по умолчанию
        CurrentPage = GetPageInstance<NewsPage>();
        ToggleNavigationCommand = new RelayCommand(o => IsNavigationExpanded = !IsNavigationExpanded);
    }

    public void NavigateTo<T>() where T : Page
    {
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
        CurrentPage = new ProfilePage(_profileViewModel);
    }

    private Page GetPageInstance<T>() where T : Page
    {
        var type = typeof(T);

        if (!_pageCache.TryGetValue(type, out var page))
        {
            if (type == typeof(ProfilePage))
            {
                page = new ProfilePage(_profileViewModel);
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
