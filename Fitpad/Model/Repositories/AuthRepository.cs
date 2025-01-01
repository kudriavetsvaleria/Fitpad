
using System.IO;
using Newtonsoft.Json;

namespace Fitpad.Model.Repositories
{
    public class AuthRepository
    {
        private const string AuthFilePath = "auth_state.json";

        public void SaveAuthState(UserModel user)
        {
            var json = JsonConvert.SerializeObject(user);
            File.WriteAllText(AuthFilePath, json);
        }

        public UserModel LoadAuthState()
        {
            if (File.Exists(AuthFilePath))
            {
                var json = File.ReadAllText(AuthFilePath);
                return JsonConvert.DeserializeObject<UserModel>(json);
            }
            return null;
        }

        public void ClearAuthState()
        {
            if (File.Exists(AuthFilePath))
            {
                File.Delete(AuthFilePath);
            }
        }
    }
}
