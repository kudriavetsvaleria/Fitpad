using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Google.Cloud.Firestore;
using Fitpad.Model.Entities;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Fitpad.View.Components
{
    public partial class DishDetailPage : Page, INotifyPropertyChanged
    {
        private bool _isEditing = false;
        private DishModel _dish;
        private FirestoreDb _firestoreDb;

        public event PropertyChangedEventHandler PropertyChanged;

        public DishDetailPage(DishModel dish)
        {
            InitializeComponent();
            _dish = dish;
            DataContext = _dish;

            _firestoreDb = FirestoreDb.Create("fitpad-2025"); // Подключение к базе данных
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = !_isEditing;
            SetEditMode(_isEditing);
        }

        private void SetEditMode(bool isEditing)
        {
            // Поля для редактирования
            DishNameBox.IsReadOnly = !isEditing;
            DishNameBox.Foreground = isEditing ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;

            CookingTimeBox.IsReadOnly = !isEditing;
            CookingTimeBox.Background = isEditing ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Transparent;

            RecipeBox.IsReadOnly = !isEditing;
            RecipeBox.Background = isEditing ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Transparent;

            IngredientsList.IsEnabled = isEditing;

            // Изменение кнопки "Назад" на "Готово"
            ActionButton.Content = isEditing ? "Готово" : "Назад";

            // Устанавливаем фокус в поле названия при включении режима редактирования
            if (isEditing)
            {
                DishNameBox.Focus();
                DishNameBox.SelectAll(); // Выделяет весь текст для удобного редактирования
            }
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditing)
            {
                var userId = UserSession.CurrentUserId;      // <-- получаем текущего юзера
                await SaveChangesToFirestore(userId);        // <-- передаём userId
                SetEditMode(false);
                _isEditing = false;
            }
            else
            {
                NavigationService?.GoBack();
            }
        }

        private async System.Threading.Tasks.Task SaveChangesToFirestore(string userId)
        {
            var dishRef = _firestoreDb
                .Collection("Users").Document(userId)
                .Collection("Dishes").Document(_dish.Id);

            var updatedData = new Dictionary<string, object>
            {
                { "Name", DishNameBox.Text },
                { "CookingTime", CookingTimeBox.Text },
                { "Recipe", RecipeBox.Text },
                { "Ingredients", _dish.Ingredients },
                { "UpdatedAt", Timestamp.FromDateTime(DateTime.UtcNow) }
            };

            await dishRef.UpdateAsync(updatedData);

            MessageBox.Show("Зміни збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
