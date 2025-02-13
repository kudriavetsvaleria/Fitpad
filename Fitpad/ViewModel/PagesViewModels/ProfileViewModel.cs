using Fitpad.Model.Entities;
using Fitpad.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
            OnPropertyChanged(); // Уведомляем интерфейс об изменении свойства
        }
    }

    public UserInfoModel CurrentUserInfo
    {
        get => _currentUserInfo;
        set
        {
            _currentUserInfo = value;
            OnPropertyChanged(); // Уведомляем интерфейс об изменении свойства
        }
    }

    public ProfileViewModel(UserModel user = null)
    {
        CurrentUser = user;
        if (user != null)
        {
            _ = LoadUserInfoAsync(user.Id); // Загружаем данные анкеты
        }
    }

    public async Task LoadUserInfoAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var firestoreService = new FirestoreService();
        var userInfo = await firestoreService.GetUserInfoAsync(userId);

        if (userInfo != null)
        {
            CurrentUserInfo = userInfo; // Обновляем данные анкеты
        }
    }

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
