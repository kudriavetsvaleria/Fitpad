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

        public async Task<string> TranslateTextAsync(string text, string targetLanguage = "uk")
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            using (var client = new HttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}?key={_apiKey}&q={Uri.EscapeDataString(text)}&target={targetLanguage}";
                    var response = await client.GetStringAsync(url);
                    var json = JObject.Parse(response);
                    var translatedText = json["data"]["translations"][0]["translatedText"].ToString();     
                    return translatedText;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при переводе: {ex.Message}");
                    return text; // Возвращаем исходный текст в случае ошибки
                }
            }
        }

    }
}
