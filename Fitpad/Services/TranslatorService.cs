using Google.Cloud.Firestore;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;

namespace Fitpad.Services
{
    public class TranslatorService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _apiKey = System.Configuration.ConfigurationManager.AppSettings["TranslatorApiKey"];
        private readonly string _baseUrl = "https://translation.googleapis.com/language/translate/v2";
        private long _totalTranslatedCharacters = 0; // Счетчик символов
        private readonly FirestoreDb _firestoreDb;
        private const string CollectionName = "TranslationStats";
        private const string DocumentId = "TotalCharacters";

        public TranslatorService()
        {
            _firestoreDb = FirestoreDbProvider.Instance.GetDb();
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
                    Logger.Info($"Загружено общее количество переведенных символов: {_totalTranslatedCharacters}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при загрузке данных из Firebase");
            }
        }

        private async Task<string> TranslateQueryAsync(string query)
        {
            try
            {
                // Вызываем TranslateTextAsync напрямую
                return await TranslateTextAsync(query, "en");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка перевода");
                return query; // Возвращаем оригинальный запрос в случае ошибки
            }
        }

        // Метод для сохранения общего количества символов в Firebase
        private async Task SaveTotalCharactersAsync()
        {
            try
            {
                var docRef = _firestoreDb.Collection(CollectionName).Document(DocumentId);
                await docRef.SetAsync(new { Total = _totalTranslatedCharacters });
                Logger.Debug($"Общее количество переведенных символов сохранено: {_totalTranslatedCharacters}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при сохранении данных в Firebase");
            }
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage = "uk")
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Увеличиваем счетчик символов
            _totalTranslatedCharacters += text.Length;
            Logger.Debug($"Общее количество переведенных символов: {_totalTranslatedCharacters}");

            using (var client = new HttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}?key={_apiKey}&q={Uri.EscapeDataString(text)}&target={targetLanguage}";
                    var response = await client.GetStringAsync(url);
                    var json = JObject.Parse(response);
                    var translatedRaw = json["data"]["translations"][0]["translatedText"].ToString();
                    var translatedText = System.Net.WebUtility.HtmlDecode(translatedRaw);


                    // Сохраняем обновленный счетчик в Firebase
                    await SaveTotalCharactersAsync();
                    return translatedText;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Ошибка при переводе");
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
