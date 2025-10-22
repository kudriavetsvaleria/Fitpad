using System.Windows;
using Fitpad.View.Pages;
using Fitpad.Services;
using Fitpad.Model.Entities;

namespace Fitpad.View.Components
{
    public partial class UserInfoWindow : Window
    {
        private readonly ProfileViewModel _vm;
        private readonly FirestoreService _fs = new FirestoreService();

        public UserInfoWindow(ProfileViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;

            Loaded += UserInfoWindow_Loaded;
        }

        private void UserInfoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_vm?.CurrentUserInfo == null) return;

            // Предзаполняем ComboBox
            SetComboBoxValue(ActivityCombo, _vm.CurrentUserInfo.ActivityLevel);
            SetComboBoxValue(PurposeCombo, _vm.CurrentUserInfo.Purpose);
        }

        private void SetComboBoxValue(System.Windows.Controls.ComboBox combo, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            foreach (var item in combo.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem cbi &&
                    cbi.Content.ToString().ToLower().Contains(value.ToLower()))
                {
                    combo.SelectedItem = cbi;
                    return;
                }
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.CurrentUser == null || _vm.CurrentUserInfo == null)
            {
                MessageBox.Show("Немає даних користувача!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _vm.CurrentUserInfo.ActivityLevel = (ActivityCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "";
            _vm.CurrentUserInfo.Purpose = (PurposeCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "";

            await _fs.SaveUserInfoAsync(_vm.CurrentUserInfo);
            MessageBox.Show("✅ Дані успішно оновлено!", "Fitpad", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Ви впевнені, що хочете вийти з акаунту?",
                                "Підтвердження",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MainViewModel.Instance.Logout();
                Close();
            }
        }
    }
}
