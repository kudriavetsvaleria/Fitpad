using System;
using System.IO;
using Fitpad.Model.Repositories;
using Newtonsoft.Json;

public static class UserSession
{
    private static string _currentUserId;

    // ✅ Унифицированный путь к файлу
    private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fitpad", "current_user.json");


    public static string CurrentUserId
    {
        get => _currentUserId;
        set
        {
            Console.WriteLine($"🔹 Изменение UserSession.CurrentUserId: {_currentUserId} → {value}");
            _currentUserId = value;
        }
    }

    public static void Logout()
{
    CurrentUserId = string.Empty;
    string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fitpad", "current_user.json");
    
    if (File.Exists(filePath))
    {
        File.Delete(filePath);
    }

    Console.WriteLine("🔹 UserSession очищен. Пользователь вышел.");
}


    // ✅ Загрузка UserID из файла
    static UserSession()
    {
        LoadUserIdFromFile();
    }

    public static void LoadUserIdFromFile()
    {
        try
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fitpad", "current_user.json");
            Console.WriteLine($"📂 Ищем файл: {filePath}");

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Console.WriteLine($"📜 Загруженные данные из current_user.json: {json}");

                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine("❌ Файл `current_user.json` пустой!");
                    return;
                }

                var data = JsonConvert.DeserializeObject<dynamic>(json);

                if (data != null && data.UserId != null)
                {
                    Console.WriteLine($"✅ UserID найден в файле: {data.UserId}");

                    // ✅ Перед установкой проверяем, что значение не пустое
                    if (!string.IsNullOrEmpty(data.UserId.ToString()))
                    {
                        UserSession.CurrentUserId = data.UserId.ToString();
                        Console.WriteLine($"✅ Установлен UserSession.CurrentUserId: {UserSession.CurrentUserId}");
                    }
                    else
                    {
                        Console.WriteLine("❌ Ошибка: UserID пустой после десериализации!");
                    }
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
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка при загрузке UserID: {ex.Message}");
        }
    }


    // ✅ Сохранение UserID в файл
    public static void SaveUserIdToFile(string userId)
    {
        CurrentUserId = userId;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); // Создаём папку, если её нет

            var data = new { UserId = userId };
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(FilePath, json);

            _currentUserId = userId;
            Console.WriteLine($"✅ UserID {userId} сохранён в файле.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка сохранения UserID в файл: {ex.Message}");
        }
    }

    // ✅ Очистка данных при выходе
    public static void ClearUserData()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
                Console.WriteLine("✅ Файл current_user.json успешно удалён.");
            }
            else
            {
                Console.WriteLine("⚠️ Файл current_user.json уже отсутствует.");
            }

            _currentUserId = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка удаления файла: {ex.Message}");
        }
    }
}
