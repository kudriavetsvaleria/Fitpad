using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Fitpad.Model;
using Fitpad.Model.Entities;
using Fitpad.View.Pages;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        private UserModel _currentUser;
        private UserInfoModel _currentUserInfo;

        public UserModel CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
            }
        }

        public UserInfoModel CurrentUserInfo
        {
            get => _currentUserInfo;
            set
            {
                _currentUserInfo = value;
                OnPropertyChanged();
            }
        }

        public ICommand LogoutCommand { get; }

        public ProfileViewModel()
        {
            LogoutCommand = new RelayCommand(Logout);
            LoadUserData(); // Загружаем данные пользователя при инициализации
        }

        public void ClearUserData()
        {
            CurrentUser = null;
            CurrentUserInfo = null;
            UserStorage.Save(null); // Очищаем сохраненные данные пользователя
            UserInfoStorage.Save(null); // Очищаем сохраненные данные анкеты
        }

        public void Logout(object obj)
        {
            // Очистка данных пользователя
            ClearUserData();

            // Показ сообщения об успешном выходе
            MessageBox.Show("Вы вышли из аккаунта.", "Выход", MessageBoxButton.OK, MessageBoxImage.Information);

            // Проверка главного окна
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                MessageBox.Show("Переход на страницу авторизации...");
                mainWindow.Content = AccountLoginPage.GetInstance(new ProfileViewModel());
            }
            else
            {
                MessageBox.Show("Ошибка: главное окно не найдено.");
            }
        }

        public void SaveUserData(UserModel user)
        {
            CurrentUser = user;
            UserStorage.Save(user); // Сохраняем данные пользователя
        }

        private void LoadUserData()
        {
            var user = UserStorage.Load();
            if (user != null)
            {
                CurrentUser = user;
                LoadUserInfoData(user.Id); // Загружаем данные анкеты пользователя
            }
        }

        private void LoadUserInfoData(int userId)
        {
            var userInfo = UserInfoStorage.Load(userId);
            if (userInfo != null)
            {
                CurrentUserInfo = userInfo;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
