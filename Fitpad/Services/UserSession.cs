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
        Console.WriteLine($"?? Ищем файл: {FilePath}");

        if (!File.Exists(FilePath))
        {
            Console.WriteLine("⚠️ Файл `current_user.json` не найден.");
            _currentUserId = null;
            return;
        }

        try
        {
            string json = File.ReadAllText(FilePath);
            Console.WriteLine($"📜 Загруженные данные из current_user.json: {json}");

            var data = JsonConvert.DeserializeObject<dynamic>(json);
            if (data != null && data.UserId != null)
            {
                Console.WriteLine($"✅ UserID найден в файле: {data.UserId}");
                _currentUserId = data.UserId.ToString();
                Console.WriteLine($"🔹 Установлен UserSession.CurrentUserId: {_currentUserId}");
            }
            else
            {
                Console.WriteLine("❌ Ошибка: данные в файле некорректны!");
                _currentUserId = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка загрузки файла current_user.json: {ex.Message}");
            _currentUserId = null;
        }
        UserSession.CurrentUserId = UserRepository.CurrentUserId;

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
