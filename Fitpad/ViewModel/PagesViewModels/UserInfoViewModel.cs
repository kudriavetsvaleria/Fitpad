using Fitpad.Model.Entities;
using Fitpad.Services;
using System.Threading.Tasks;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class UserInfoViewModel
    {
        private readonly UserModel _currentUser;
        private readonly FirestoreService _firestoreService;

        public UserInfoModel CurrentUserInfo { get; private set; }

        public UserInfoViewModel(UserModel user)
        {
            _currentUser = user;
            _firestoreService = new FirestoreService();
            _ = LoadUserInfoAsync().ConfigureAwait(false);
        }

        public bool IsUserInfoComplete()
        {
            return CurrentUserInfo != null
                   && CurrentUserInfo.Age > 0
                   && CurrentUserInfo.Height > 0
                   && CurrentUserInfo.Weight > 0;
        }

        private async Task LoadUserInfoAsync()
        {
            CurrentUserInfo = await _firestoreService.GetUserInfoAsync(_currentUser.Id).ConfigureAwait(false);

            if (CurrentUserInfo == null)
            {
                await CreateEmptyUserInfoAsync(_currentUser.Id).ConfigureAwait(false);
                CurrentUserInfo = await _firestoreService.GetUserInfoAsync(_currentUser.Id).ConfigureAwait(false);
            }
        }

        public async Task CreateEmptyUserInfoAsync(string userId)
        {
            var newUserInfo = new UserInfoModel
            {
                UserId = userId,
                Gender = "Не указано",
                Age = 0,
                Height = 0,
                Weight = 0,
                ActivityLevel = "Не указано"
            };

            await _firestoreService.SaveUserInfoAsync(newUserInfo).ConfigureAwait(false);
        }

        public bool HasUserInfo()
        {
            return CurrentUserInfo != null
                   && CurrentUserInfo.Age > 0
                   && CurrentUserInfo.Height > 0
                   && CurrentUserInfo.Weight > 0;
        }
    }
}