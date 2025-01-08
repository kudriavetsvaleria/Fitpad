using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using System.Threading.Tasks;

namespace Fitpad.Services
{
    public class RegistrationService
    {
        private readonly UserRepository _userRepository;
        private readonly UserInfoRepository _userInfoRepository;

        public RegistrationService()
        {
            _userRepository = new UserRepository();
            _userInfoRepository = new UserInfoRepository();
        }

        public async Task RegisterUserAsync(UserModel user)
        {
            await _userRepository.SaveUserAsync(user); // Сохранение пользователя

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

            await _userInfoRepository.SaveUserInfoAsync(userInfo); // Создание пустой анкеты
        }
    }
}
