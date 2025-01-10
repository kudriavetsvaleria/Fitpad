using Fitpad.Model.Entities;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

        public ProfileViewModel(UserModel user = null)
        {
            CurrentUser = user;
        }

        // Метод для обновления данных пользователя
        public void UpdateUserData(UserModel updatedUser)
        {
            if (updatedUser != null)
            {
                CurrentUser = updatedUser;
            }
        }

        public void ClearUserData()
        {
            CurrentUser = null;
            CurrentUserInfo = null;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
