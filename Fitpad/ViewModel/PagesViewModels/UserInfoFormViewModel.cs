using Fitpad.Model;
using Fitpad.Model.Entities;
using System.Linq;
using System.Windows;

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
            {
                MessageBox.Show("Проверьте правильность введенных данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            using (var context = new ApplicationDbContext())
            {
                // Проверяем наличие записи для текущего пользователя
                var existingUserInfo = context.UserInfos.FirstOrDefault(info => info.UserId == _currentUser.Id);

                if (existingUserInfo == null)
                {
                    MessageBox.Show("Ошибка: запись пользователя не найдена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Обновляем данные анкеты
                existingUserInfo.Gender = gender;
                existingUserInfo.Age = age;
                existingUserInfo.Height = height;
                existingUserInfo.Weight = weight;
                existingUserInfo.ActivityLevel = activityLevel;

                context.SaveChanges(); // Сохраняем изменения
            }

            return true;
        }

    }
}
