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
            // Проверка корректности введенных данных
            if (!int.TryParse(ageText, out int age) || !int.TryParse(heightText, out int height) || !double.TryParse(weightText, out double weight))
                return false;

            using (var context = new ApplicationDbContext())
            {
                // Проверяем, есть ли уже данные для текущего пользователя
                var existingUserInfo = context.UserInfos.FirstOrDefault(info => info.UserId == _currentUser.Id);

                if (existingUserInfo == null)
                {
                    // Если данных нет, создаем новую запись
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
                    // Если данные уже существуют, обновляем их
                    existingUserInfo.Gender = gender;
                    existingUserInfo.Age = age;
                    existingUserInfo.Height = height;
                    existingUserInfo.Weight = weight;
                    existingUserInfo.ActivityLevel = activityLevel;
                }

                context.SaveChanges(); // Сохраняем изменения в базе данных
            }

            return true;
        }
    }
}
