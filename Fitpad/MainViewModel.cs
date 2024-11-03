using Fitpad.View.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;

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
    public ICommand ToggleNavigationCommand { get; }

    public MainViewModel()
    {
        ShowNewsCommand = new RelayCommand(o => NavigateTo<NewsPage>());
        ShowFavoritesCommand = new RelayCommand(o => NavigateTo<FavoritesPage>());
        ShowNutritionCommand = new RelayCommand(o => NavigateTo<NutritionPage>());
        ShowWorkoutsCommand = new RelayCommand(o => NavigateTo<WorkoutsPage>());
        ShowProfileCommand = new RelayCommand(o => NavigateTo<ProfilePage>());

        // Открываем страницу новостей по умолчанию
        CurrentPage = GetPageInstance<NewsPage>();
    }

    private void NavigateTo<T>() where T : Page, new()
    {
        CurrentPage = GetPageInstance<T>();
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
}
