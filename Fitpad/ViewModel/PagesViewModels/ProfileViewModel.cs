using System.ComponentModel;
using System.Linq;
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

            // Удаляем данные из хранилища
            UserStorage.Clear();
            UserInfoStorage.Clear();
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
                LoadUserInfoData(user.Id); // Загружаем данные анкеты для текущего пользователя
            }
            else
            {
                // Убираем сообщение об ошибке, так как отсутствие данных – это нормальная ситуация при первом запуске
                CurrentUser = null;
                CurrentUserInfo = null;
            }
        }

        private void LoadUserInfoData(int userId)
        {
            using (var context = new ApplicationDbContext())
            {
                var userInfo = context.UserInfos.FirstOrDefault(info => info.UserId == userId);
                if (userInfo != null)
                {
                    CurrentUserInfo = userInfo;
                }
                else
                {
                    // Если данных анкеты нет, создаем пустую модель, чтобы избежать ошибок
                    CurrentUserInfo = new UserInfoModel
                    {
                        UserId = userId,
                        Gender = "Не указано",
                        Age = 0,
                        Height = 0,
                        Weight = 0,
                        ActivityLevel = "Не указано"
                    };
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
