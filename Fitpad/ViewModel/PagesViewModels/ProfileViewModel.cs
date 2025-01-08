using System;
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
        public ProfileViewModel()
        {
            LoadUserData();
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

        // Новый конструктор с параметром UserModel
        public ProfileViewModel(UserModel user)
        {
            CurrentUser = user;
            LoadUserInfoData(user.Id); // Загружаем данные анкеты для авторизованного пользователя
        }

        public void ClearUserData()
        {
            CurrentUser = null;
            CurrentUserInfo = null;
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

        public void UpdateUserData(UserModel user)
        {
            if (user == null) return;

            CurrentUser = user;
            LoadUserInfoData(user.Id); // Загружаем актуальные данные анкеты
        }


        private void LoadUserInfoData(int userId)
        {
            using (var context = new ApplicationDbContext())
            {
                var userInfo = context.UserInfos.FirstOrDefault(info => info.UserId == userId);
                if (userInfo != null)
                {
                    CurrentUserInfo = userInfo;
                    Console.WriteLine("Данные анкеты загружены.");
                }
                else
                {
                    CurrentUserInfo = new UserInfoModel
                    {
                        UserId = userId,
                        Gender = "Не указано",
                        Age = 0,
                        Height = 0,
                        Weight = 0,
                        ActivityLevel = "Не указано"
                    };
                    Console.WriteLine("Данные анкеты отсутствуют.");
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
