using Fitpad.Model;
using Fitpad.Model.Entities;
using System.Linq;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class UserInfoViewModel
    {
        private readonly UserModel _currentUser;

        public UserInfoModel CurrentUserInfo { get; private set; }

        public UserInfoViewModel(UserModel user)
        {
            _currentUser = user;
            LoadUserInfo(); // Загружаем данные пользователя при создании экземпляра
        }

        private void LoadUserInfo()
        {
            using (var context = new ApplicationDbContext())
            {
                CurrentUserInfo = context.UserInfos.FirstOrDefault(info => info.UserId == _currentUser.Id);
            }
        }

        public bool HasUserInfo()
        {
            return CurrentUserInfo != null && CurrentUserInfo.Age > 0 && CurrentUserInfo.Height > 0 && CurrentUserInfo.Weight > 0;
        }
    }
}
