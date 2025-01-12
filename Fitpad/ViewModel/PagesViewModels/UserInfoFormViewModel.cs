using Fitpad.Model.Entities;
using Fitpad.Services;
using System.Threading.Tasks;
using System.Windows;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class UserInfoFormViewModel
    {
        private readonly UserModel _currentUser;
        private readonly FirestoreService _firestoreService;

        public UserInfoFormViewModel(UserModel user)
        {
            _currentUser = user;
            _firestoreService = new FirestoreService();
        }

        public async Task<bool> SaveUserInfoAsync(string gender, string ageText, string heightText, string weightText, string activityLevel)
        {
            if (!int.TryParse(ageText, out int age) || !int.TryParse(heightText, out int height) || !double.TryParse(weightText, out double weight))
            {
                MessageBox.Show("Перевірте правильність введених даних.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var existingUserInfo = await _firestoreService.GetUserInfoAsync(_currentUser.Id).ConfigureAwait(false);

            if (existingUserInfo == null)
            {
                MessageBox.Show("Помилка: запис користувача не знайдено.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            existingUserInfo.Gender = gender;
            existingUserInfo.Age = age;
            existingUserInfo.Height = height;
            existingUserInfo.Weight = weight;
            existingUserInfo.ActivityLevel = activityLevel;

            await _firestoreService.SaveUserInfoAsync(existingUserInfo).ConfigureAwait(false);
            return true;
        }
    }
}