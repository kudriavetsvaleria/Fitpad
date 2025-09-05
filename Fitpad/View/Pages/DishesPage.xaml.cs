using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Fitpad.Model.Entities;
using Fitpad.Services;
using Fitpad.View.Components;
using Fitpad.ViewModel.PagesViewModels;
using Google.Cloud.Firestore;

namespace Fitpad.View.Pages
{
    public partial class DishesPage : Page
    {
        private readonly DishViewModel _viewModel;
        private readonly FirestoreService _firestoreService;

        public DishesPage()
        {
            InitializeComponent();
            _firestoreService = new FirestoreService();
            _viewModel = new DishViewModel(FirestoreDb.Create("fitpad-2025"));
            DataContext = _viewModel;

            _viewModel.LoadUserDishesAsync(UserSession.CurrentUserId);
            CreateDishButton.DataContext = MainViewModel.Instance;

       
        }

        // 🔍 Обработчик поиска
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(query))
            {
                DishesList.ItemsSource = _viewModel.Dishes;
            }
            else
            {
                var filteredDishes = _viewModel.Dishes
                    .Where(d => d.Name.ToLower().Contains(query))
                    .ToList();
                DishesList.ItemsSource = filteredDishes;
            }
        }

        // 🔄 Имитация Placeholder (так как WPF не поддерживает PlaceholderText)
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Пошук страв...")
            {
                SearchBox.Text = "";
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Пошук страв...";
            }
        }

        // ✅ Обработчик выбора блюда (включает кнопку удаления)
        private void DishesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DishesList.SelectedItem is DishModel selectedDish)
            {
                Console.WriteLine($"📌 Выбрано блюдо: {selectedDish.Name}");
                NavigationService?.Navigate(new DishDetailPage(selectedDish));
            }
        }


        private async void ToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DishModel dish)
            {
                dish.IsFavorite = !dish.IsFavorite;
                var userId = UserSession.CurrentUserId;

                try
                {
                    await _firestoreService.UpdateFavoriteStatus(userId, dish.Id, dish.IsFavorite);
                    DishesList.Items.Refresh();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка обновления избранного: {ex.Message}");
                }
            }
        }


        public void RefreshDishesList()
        {
            _viewModel.LoadUserDishesAsync(UserSession.CurrentUserId);
            DishesList.ItemsSource = null;
            DishesList.ItemsSource = _viewModel.Dishes;

            Console.WriteLine("🔄 Список страв оновлено!");
        }

        // 🗑 Удаление блюда
        private async void DeleteDish_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DishModel dish)
            {
                if (MessageBox.Show($"Ви впевнені, що хочете видалити '{dish.Name}'?",
                                    "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var userId = UserSession.CurrentUserId;

                    try
                    {
                        await _firestoreService.DeleteDishFromFirebase(userId, dish.Id);
                        _viewModel.Dishes.Remove(dish);
                        DishesList.Items.Refresh();
                        Console.WriteLine($"✅ Блюдо '{dish.Name}' видалено.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Помилка видалення: {ex.Message}");
                    }
                }
            }
        }


    }
}
