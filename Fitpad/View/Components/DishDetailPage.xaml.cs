using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Google.Cloud.Firestore;
using Fitpad.Model.Entities;

namespace Fitpad.View.Components
{
    public partial class DishDetailPage : Page, INotifyPropertyChanged
    {
        private bool _isEditing = false;
        private readonly DishModel _dish;
        private readonly FirestoreDb _firestoreDb;

        public event PropertyChangedEventHandler PropertyChanged;

        // --- правила валідації ---
        // Назва: 2–60 символів, літери (укр/лат), цифри, пробіли, - ' . ,
        private static readonly Regex NameRx =
            new Regex(@"^[A-Za-zА-Яа-яІіЇїЄє0-9 \-'\.,]{2,60}$", RegexOptions.Compiled);

        public DishDetailPage(DishModel dish)
        {
            InitializeComponent();
            _dish = dish;
            DataContext = _dish;

            _firestoreDb = FirestoreDb.Create("fitpad-2025");
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = !_isEditing;
            SetEditMode(_isEditing);
        }

        private void SetEditMode(bool isEditing)
        {
            DishNameBox.IsReadOnly = !isEditing;
            DishNameBox.Foreground = isEditing ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;

            CookingTimeBox.IsReadOnly = !isEditing;
            CookingTimeBox.Background = isEditing ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Transparent;

            RecipeBox.IsReadOnly = !isEditing;
            RecipeBox.Background = isEditing ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Transparent;

            IngredientsList.IsEnabled = isEditing;

            ActionButton.Content = isEditing ? "Готово" : "Назад";

            if (isEditing)
            {
                DishNameBox.Focus();
                DishNameBox.SelectAll();
            }
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditing)
            {
                // ✅ валідація перед збереженням
                if (!ValidateBeforeSave()) return;

                var userId = UserSession.CurrentUserId;
                await SaveChangesToFirestore(userId);
                SetEditMode(false);
                _isEditing = false;
            }
            else
            {
                NavigationService?.GoBack();
            }
        }

        // ---------- ВАЛІДАЦІЯ ----------
        private bool ValidateBeforeSave()
        {
            // нормалізуємо назву (обрізка зайвих пробілів усередині)
            var normalizedName = NormalizeSpaces(DishNameBox.Text ?? "");
            DishNameBox.Text = normalizedName;

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                MessageBox.Show("Вкажіть назву страви.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DishNameBox.Focus();
                return false;
            }
            if (!NameRx.IsMatch(normalizedName))
            {
                MessageBox.Show("Некоректна назва. Дозволені: літери, цифри, пробіли, «-», «'», «.», «,». Довжина 2–60 символів.",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DishNameBox.Focus();
                DishNameBox.SelectAll();
                return false;
            }

            // Час приготування — дістаємо тільки цифри (допускаємо «45 хв», «30min» тощо)
            var digitsOnly = ExtractDigits(CookingTimeBox.Text ?? "");
            if (string.IsNullOrEmpty(digitsOnly))
            {
                MessageBox.Show("Вкажіть час приготування у хвилинах.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                CookingTimeBox.Focus();
                CookingTimeBox.SelectAll();
                return false;
            }

            if (!int.TryParse(digitsOnly, out int minutes) || minutes <= 0)
            {
                MessageBox.Show("Час приготування має бути додатним числом.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                CookingTimeBox.Focus();
                CookingTimeBox.SelectAll();
                return false;
            }

            // Обмежимо адекватним діапазоном, наприклад 1–600 хв (10 годин)
            if (minutes > 600)
            {
                MessageBox.Show("Занадто великий час приготування. Вкажіть значення до 600 хвилин.",
                                "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                CookingTimeBox.Focus();
                CookingTimeBox.SelectAll();
                return false;
            }

            // Перезаписуємо поле у нормальному вигляді, наприклад "45"
            CookingTimeBox.Text = minutes.ToString();

            return true;
        }

        private static string NormalizeSpaces(string s)
        {
            s = (s ?? "").Trim();
            // заміна послідовностей пробілів одним пробілом
            return Regex.Replace(s, @"\s{2,}", " ");
        }

        private static string ExtractDigits(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return Regex.Replace(s, @"\D+", ""); // лишаємо тільки цифри
        }

        // ---------- ЗБЕРЕЖЕННЯ ----------
        private async System.Threading.Tasks.Task SaveChangesToFirestore(string userId)
        {
            var dishRef = _firestoreDb
                .Collection("Users").Document(userId)
                .Collection("Dishes").Document(_dish.Id);

            var updatedData = new Dictionary<string, object>
            {
                { "Name",        DishNameBox.Text },
                { "CookingTime", CookingTimeBox.Text }, // уже нормалізовано (цифри)
                { "Recipe",      RecipeBox.Text },
                { "Ingredients", _dish.Ingredients },
                { "UpdatedAt",   Timestamp.FromDateTime(DateTime.UtcNow) }
            };

            await dishRef.UpdateAsync(updatedData);

            MessageBox.Show("Зміни збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

            // оновимо локальну модель, щоб DataContext був консистентний
            _dish.Name = DishNameBox.Text;
            _dish.CookingTime = CookingTimeBox.Text;
            _dish.Recipe = RecipeBox.Text;
            OnPropertyChanged(nameof(_dish));
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
