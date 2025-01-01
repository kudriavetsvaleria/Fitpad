using System.ComponentModel;
using System.Runtime.CompilerServices;
using Fitpad.Model;

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

        public ProfileViewModel()
        {
            LoadUserData();
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
