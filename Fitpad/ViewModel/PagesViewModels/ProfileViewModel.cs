using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Fitpad.Model;
using Fitpad.View.Pages;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        private UserModel _currentUser;

        public UserModel CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
            }
        }

        public ICommand LogoutCommand { get; }

        public ProfileViewModel()
        {
            LogoutCommand = new RelayCommand(Logout);
            LoadUserData(); // Загружаем данные пользователя при инициализации
        }

        private void Logout(object obj)
        {
            // Очистка данных пользователя
            CurrentUser = null;
            UserStorage.Save(null);

            // Показ сообщения об успешном выходе
            MessageBox.Show("Вы вышли из аккаунта.", "Выход", MessageBoxButton.OK, MessageBoxImage.Information);

            // Создаем новую страницу входа
            var loginPage = new AccountLoginPage(new ProfileViewModel());

            // Устанавливаем новую страницу в главном окне
            App.Current.MainWindow.Content = loginPage;
        }

        public void SaveUserData(UserModel user)
        {
            CurrentUser = user;
            UserStorage.Save(user); // Сохраняем данные в файл
        }

        private void LoadUserData()
        {
            var user = UserStorage.Load();
            if (user != null)
            {
                CurrentUser = user;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
