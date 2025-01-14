using Google.Cloud.Firestore;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Fitpad.Services
{
    public class TranslatorService
    {
        private readonly string _apiKey = "AIzaSyDb04TkY9CwJkCcOHTfvsXFpeu6xxB7LFI";
        private readonly string _baseUrl = "https://translation.googleapis.com/language/translate/v2";
        private long _totalTranslatedCharacters = 0; // Счетчик символов
        private readonly FirestoreDb _firestoreDb;
        private const string CollectionName = "TranslationStats";
        private const string DocumentId = "TotalCharacters";

        public TranslatorService()
        {
            _firestoreDb = FirestoreDb.Create("fitpad-2025"); // Инициализация Firebase
            _ = LoadTotalCharactersAsync(); // Асинхронная загрузка данных при создании экземпляра
        }

        // Метод для загрузки общего количества символов из Firebase
        private async Task LoadTotalCharactersAsync()
        {
            try
            {
                var docRef = _firestoreDb.Collection(CollectionName).Document(DocumentId);
                var snapshot = await docRef.GetSnapshotAsync();
                if (snapshot.Exists && snapshot.ContainsField("Total"))
                {
                    _totalTranslatedCharacters = snapshot.GetValue<long>("Total");
                    Console.WriteLine($"Загружено общее количество переведенных символов: {_totalTranslatedCharacters}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке данных из Firebase: {ex.Message}");
            }
        }

        // Метод для сохранения общего количества символов в Firebase
        private async Task SaveTotalCharactersAsync()
        {
            try
            {
                var docRef = _firestoreDb.Collection(CollectionName).Document(DocumentId);
                await docRef.SetAsync(new { Total = _totalTranslatedCharacters });
                Console.WriteLine($"Общее количество переведенных символов сохранено: {_totalTranslatedCharacters}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении данных в Firebase: {ex.Message}");
            }
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage = "uk")
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Увеличиваем счетчик символов
            _totalTranslatedCharacters += text.Length;
            Console.WriteLine($"Общее количество переведенных символов: {_totalTranslatedCharacters}");

            using (var client = new HttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}?key={_apiKey}&q={Uri.EscapeDataString(text)}&target={targetLanguage}";
                    var response = await client.GetStringAsync(url);
                    var json = JObject.Parse(response);
                    var translatedText = json["data"]["translations"][0]["translatedText"].ToString();

                    // Сохраняем обновленный счетчик в Firebase
                    await SaveTotalCharactersAsync();
                    return translatedText;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при переводе: {ex.Message}");
                    return text; // Возвращаем исходный текст в случае ошибки
                }
            }
        }

        // Метод для получения общего количества переведенных символов
        public long GetTotalTranslatedCharacters()
        {
            return _totalTranslatedCharacters;
        }
    }
}
