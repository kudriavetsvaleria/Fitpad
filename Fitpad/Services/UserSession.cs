using System;
using System.IO;
using Newtonsoft.Json;

namespace Fitpad.Services
{
    public static class UserSession
    {
        private static string _currentUserId;

        public static string CurrentUserId
        {
            get => _currentUserId;
            set
            {
                Console.WriteLine($"🔹 Изменение UserSession.CurrentUserId: {_currentUserId} → {value}");
                _currentUserId = value;
            }
        }

        static UserSession()
        {
            LoadUserIdFromFile();
        }

        public static void LoadUserIdFromFile()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fitpad", "current_user.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Console.WriteLine($"📜 Загруженные данные из current_user.json: {json}");

                var data = JsonConvert.DeserializeObject<dynamic>(json);
                if (data != null && data.UserId != null)
                {
                    Console.WriteLine($"✅ UserID найден в файле: {data.UserId}");
                    CurrentUserId = data.UserId.ToString();
                    Console.WriteLine($"🔹 Установлен UserSession.CurrentUserId: {CurrentUserId}");
                }
                else
                {
                    Console.WriteLine("❌ Ошибка: данные в файле некорректны!");
                }
            }
            else
            {
                Console.WriteLine("❌ Файл `current_user.json` не найден.");
            }
        }
    }
}
