using Fitpad.Model.Entities;
using Fitpad.Services;
using System.Threading.Tasks;

namespace Fitpad.Services
{
    public class RegistrationService
    {
        private readonly FirestoreService _firestoreService;

        public RegistrationService()
        {
            _firestoreService = new FirestoreService();
        }

        public async Task RegisterUserAsync(UserModel user)
        {
            await _firestoreService.SaveUserAsync(user).ConfigureAwait(false); // Сохранение пользователя

            var userInfo = new UserInfoModel
            {
                UserId = user.Id,
                Gender = "Не указано",
                Age = 0,
                Height = 0,
                Weight = 0,
                ActivityLevel = "Не указано",
                Purpose = "Не указано"
            };

            await _firestoreService.SaveUserInfoAsync(userInfo).ConfigureAwait(false); // Создание пустой анкеты
        }

    }
}