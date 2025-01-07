using System.IO;
using Newtonsoft.Json;
using Fitpad.Model.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Fitpad.Model
{
    public static class UserInfoStorage
    {
        private static readonly string FilePath = "UserInfoData.json";

        public static UserInfoModel Load(int userId)
        {
            if (!File.Exists(FilePath))
                return null;

            var json = File.ReadAllText(FilePath);
            var allUserInfo = JsonConvert.DeserializeObject<List<UserInfoModel>>(json);
            return allUserInfo?.FirstOrDefault(info => info.UserId == userId);
        }

        public static void Clear()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath); // Удаляем файл с данными анкеты
            }
        }


        public static void Save(UserInfoModel userInfo)
        {
            if (userInfo == null)
            {
                // Если передан null, удаляем файл с данными
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                return;
            }

            List<UserInfoModel> allUserInfo = new List<UserInfoModel>();
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                allUserInfo = JsonConvert.DeserializeObject<List<UserInfoModel>>(json) ?? new List<UserInfoModel>();
            }

            allUserInfo.RemoveAll(info => info.UserId == userInfo.UserId); // Удаляем старую запись
            allUserInfo.Add(userInfo);

            var newJson = JsonConvert.SerializeObject(allUserInfo, Formatting.Indented);
            File.WriteAllText(FilePath, newJson);
        }

    }
}
