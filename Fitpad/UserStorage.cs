using System.IO;
using Newtonsoft.Json;

namespace Fitpad.Model
{
    public static class UserStorage
    {
        private static readonly string FilePath = "UserData.json";

        public static UserModel Load()
        {
            if (!File.Exists(FilePath))
                return null;

            var json = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<UserModel>(json);
        }

        public static void Save(UserModel user)
        {
            var json = JsonConvert.SerializeObject(user);
            File.WriteAllText(FilePath, json);
        }
    }
}
