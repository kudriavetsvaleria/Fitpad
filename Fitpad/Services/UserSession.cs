using Newtonsoft.Json;
using System;
using System.IO;

public static class UserSession
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fitpad");
    private static readonly string FilePath = Path.Combine(Dir, "current_user.json");

    public static string CurrentUserId { get; private set; } = string.Empty;

    public static void SaveUserIdToFile(string userId)
    {
        try
        {
            if (!Directory.Exists(Dir))
                Directory.CreateDirectory(Dir);

            var payload = new { UserId = userId }; // простой формат
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(payload, Formatting.Indented));

            CurrentUserId = userId;
            Console.WriteLine($"✅ UserId сохранён: {FilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка сохранения UserId: {ex.Message}");
        }
    }

    public static void LoadUserIdFromFile()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Console.WriteLine("ℹ️ current_user.json не найден (первый запуск?).");
                CurrentUserId = string.Empty;
                return;
            }

            var json = File.ReadAllText(FilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("❌ current_user.json пустой.");
                CurrentUserId = string.Empty;
                return;
            }

            dynamic data = JsonConvert.DeserializeObject(json);
            CurrentUserId = data?.UserId != null ? (string)data.UserId : string.Empty;

            Console.WriteLine($"✅ UserId загружен: {CurrentUserId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка чтения UserId: {ex.Message}");
            CurrentUserId = string.Empty;
        }
    }

    public static void Logout()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);

            CurrentUserId = string.Empty;
            Console.WriteLine("🚪 Logout: файл current_user.json удалён, сессия очищена.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка очистки сессии: {ex.Message}");
        }
    }



}
