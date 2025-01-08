using Fitpad.Model;
using Fitpad.Model.Entities;
using System.Linq;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class UserInfoFormViewModel
    {
        private readonly UserModel _currentUser;

        public UserInfoFormViewModel(UserModel user)
        {
            _currentUser = user;
        }

        public bool SaveUserInfo(string gender, string ageText, string heightText, string weightText, string activityLevel)
        {
            if (!int.TryParse(ageText, out int age) || !int.TryParse(heightText, out int height) || !double.TryParse(weightText, out double weight))
                return false;

            using (var context = new ApplicationDbContext())
            {
                var existingUserInfo = context.UserInfos.FirstOrDefault(info => info.UserId == _currentUser.Id);

                if (existingUserInfo == null)
                {
                    var newUserInfo = new UserInfoModel
                    {
                        UserId = _currentUser.Id,
                        Gender = gender,
                        Age = age,
                        Height = height,
                        Weight = weight,
                        ActivityLevel = activityLevel
                    };
                    context.UserInfos.Add(newUserInfo);
                }
                else
                {
                    existingUserInfo.Gender = gender;
                    existingUserInfo.Age = age;
                    existingUserInfo.Height = height;
                    existingUserInfo.Weight = weight;
                    existingUserInfo.ActivityLevel = activityLevel;
                }

                context.SaveChanges();
            }

            return true;
        }
    }
}
