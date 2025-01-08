using Fitpad.Model.Entities;
using Google.Cloud.Firestore;
using System.Threading.Tasks;

namespace Fitpad.Model.Repositories
{
    public class UserInfoRepository
    {
        private readonly CollectionReference _userInfosCollection;

        public UserInfoRepository()
        {
            var db = FirestoreDb.Create("fitpad-2025");
            _userInfosCollection = db.Collection("UserInfos");
        }

        public async Task<UserInfoModel> GetUserInfoAsync(int userId)
        {
            var documentSnapshot = await _userInfosCollection.Document(userId.ToString()).GetSnapshotAsync();
            if (!documentSnapshot.Exists) return null;

            return documentSnapshot.ConvertTo<UserInfoModel>();
        }

        public async Task SaveUserInfoAsync(UserInfoModel userInfo)
        {
            await _userInfosCollection.Document(userInfo.UserId.ToString()).SetAsync(userInfo);
        }
    }
}
