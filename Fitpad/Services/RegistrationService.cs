using Fitpad.Model.Entities;
using Fitpad.Services;
using System.Threading.Tasks;
using System;                    
using Google.Cloud.Firestore;
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
            user.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow); // <== добавить поле в модель UserModel
            await _firestoreService.SaveUserAsync(user).ConfigureAwait(false);

            var userInfo = new UserInfoModel
            {
                UserId = user.Id,
                Gender = "Не вказано",
                Age = 0,
                Height = 0,
                Weight = 0,
                ActivityLevel = "Не вказано",
                Purpose = "Не вказано"
            };

            await _firestoreService.SaveUserInfoAsync(userInfo).ConfigureAwait(false);
        }


    }
}