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
using System.Text.RegularExpressions;
using System.Windows.Input; 


namespace Fitpad.View.Pages
{
    public partial class DishesPage : Page
    {
        private readonly DishViewModel _viewModel;
        private readonly FirestoreService _firestoreService;
        // Дозволені: літери (укр+лат), цифри, пробіл, -, ', .
        private static readonly Regex QueryRx =
            new Regex(@"^[A-Za-zА-Яа-яІіЇїЄє0-9 \-'.]{2,40}$", RegexOptions.Compiled);

        public DishesPage()
        {
            InitializeComponent();
            _firestoreService = new FirestoreService();
            _viewModel = new DishViewModel(FirestoreDbProvider.Instance.GetDb());
            DataContext = _viewModel;

            _viewModel.LoadUserDishesAsync(UserSession.CurrentUserId);
            CreateDishButton.DataContext = MainViewModel.Instance;

       
        }
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var raw = (SearchBox.Text ?? "").Trim();

            // ігноруємо плейсхолдер
            if (string.IsNullOrEmpty(raw) || raw == "Пошук страв...")
            {
                MessageBox.Show("Введіть назву страви для пошуку.", "Пошук", MessageBoxButton.OK, MessageBoxImage.Information);
                DishesList.ItemsSource = _viewModel.Dishes;
                return;
            }

            // перевірка на спецсимволи/довжину
            if (!QueryRx.IsMatch(raw))
            {
                MessageBox.Show("Некоректний запит. Дозволені: літери, цифри, пробіли, «-», «'», «.» (2–40 символів).",
                                "Некоректний ввід", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var q = raw.ToLower();
            var filtered = _viewModel.Dishes
                .Where(d => (d?.Name ?? string.Empty).ToLower().Contains(q))
                .ToList();

            if (filtered.Count == 0)
            {
                MessageBox.Show($"Страв не знайдено за запитом «{raw}».", "Пошук", MessageBoxButton.OK, MessageBoxImage.Information);
                DishesList.ItemsSource = _viewModel.Dishes; // або залишити порожнім списком, якщо так потрібно
                return;
            }

            DishesList.ItemsSource = filtered;
        }
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchButton_Click(sender, e);
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

        // Обработчик выбора блюда (включает кнопку удаления)
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
                    Console.WriteLine($"Ошибка обновления избранного: {ex.Message}");
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
                        Console.WriteLine($"Блюдо '{dish.Name}' видалено.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Помилка видалення: {ex.Message}");
                    }
                }
            }
        }


    }
}
