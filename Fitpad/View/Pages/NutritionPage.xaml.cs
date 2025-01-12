using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fitpad.Model.Entities;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad.View.Pages
{
    public partial class NutritionPage : Page
    {
        private readonly NutritionViewModel _viewModel;

        public NutritionPage()
        {
            InitializeComponent();

            // Включение физической прокрутки для ListView
            var scrollViewer = FindScrollViewer(MyListView);
            if (scrollViewer != null)
            {
                scrollViewer.CanContentScroll = false; // Отключаем логическую прокрутку
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged; // Привязываем обработчик события
            }

            _viewModel = new NutritionViewModel();
            DataContext = _viewModel;

            // Загрузка данных, если список пуст
            if (_viewModel.NutritionCards.Count == 0)
            {
                var random = new Random();
                int offset = random.Next(0, 1000); // Диапазон для смещения
                _ = _viewModel.LoadNutritionAsync(false, offset); // Асинхронная загрузка
            }
        }

        private async void OnSearchButtonClick(object sender, RoutedEventArgs e)
        {
            string query = SearchTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(query) && query != "Введіть запит")
            {
                await _viewModel.SearchNutritionAsync(query);
            }
            else
            {
                MessageBox.Show("Введіть запит для пошуку!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox.Text == "Введіть запит")
            {
                textBox.Text = string.Empty;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Введіть запит";
            }
        }



        private ScrollViewer FindScrollViewer(DependencyObject obj)
        {
            if (obj is ScrollViewer) return (ScrollViewer)obj;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var result = FindScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset >= e.ExtentHeight - e.ViewportHeight)
            {
                int offset = _viewModel.NutritionCards.Count;
                await _viewModel.LoadMoreNutritionAsync(offset);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            var random = new Random();
            int offset = random.Next(0, 1000); // Новый диапазон для обновления
            await _viewModel.LoadNutritionAsync(false, offset);
        }

        private void Card_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is NutritionModel model)
            {
                // Переход на страницу рецепта
                NavigationService.Navigate(new RecipePage(model));
            }
        }
    }
}
