using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Fitpad.ViewModel.PagesViewModels;
using Google.Cloud.Firestore;

namespace Fitpad.View.Pages
{
    public partial class DishesPage : Page
    {
        private readonly DishViewModel _viewModel;

        public DishesPage()
        {
            InitializeComponent();
            _viewModel = new DishViewModel(FirestoreDb.Create("fitpad-2025"));
            DataContext = _viewModel;
            _viewModel.LoadUserDishesAsync();
        }

        // 🔍 Обработчик поиска
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(query))
            {
                DishesList.ItemsSource = _viewModel.Dishes; // Вернуть весь список
            }
            else
            {
                var filteredDishes = _viewModel.Dishes.Where(d => d.Name.ToLower().Contains(query)).ToList();
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
    }
}
