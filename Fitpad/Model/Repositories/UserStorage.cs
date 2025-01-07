using System.IO;
using Newtonsoft.Json;

namespace Fitpad.Model
{
    public static class UserStorage
    {
        private static readonly string FilePath = "UserData.json";

        // Метод для получения текущего авторизованного пользователя (новый метод)
        public static UserModel GetCurrentUser()
        {
            return Load(); // Используем метод Load для получения пользователя
        }

        // Восстановленный метод Load для совместимости со старым кодом
        public static UserModel Load()
        {
            if (!File.Exists(FilePath))
                return null;

            var json = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<UserModel>(json);
        }

        // Метод для сохранения данных авторизованного пользователя
        public static void Save(UserModel user)
        {
            var json = JsonConvert.SerializeObject(user, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }

        // Метод для выхода из аккаунта (удаление файла с данными пользователя)
        public static void Logout()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }
}
