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

        public void CreateEmptyUserInfo(int userId)
        {
            using (var context = new ApplicationDbContext())
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

                context.UserInfos.Add(newUserInfo);
                context.SaveChanges();
            }
        }



        public bool IsUserInfoComplete()
        {
            return CurrentUserInfo != null
                   && CurrentUserInfo.Age > 0
                   && CurrentUserInfo.Height > 0
                   && CurrentUserInfo.Weight > 0;
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
