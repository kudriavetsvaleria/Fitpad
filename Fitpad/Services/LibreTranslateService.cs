using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Fitpad.Services
{
    public class LibreTranslateService
    {
        private readonly string _endpoint = "https://libretranslate.de/translate";

        public async Task<string> TranslateTextAsync(string text, string targetLanguage = "uk", string sourceLanguage = "en")
        {
            using var client = new HttpClient();

            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("q", text),
                new KeyValuePair<string, string>("source", sourceLanguage),
                new KeyValuePair<string, string>("target", targetLanguage),
                new KeyValuePair<string, string>("format", "text")
            });

            var response = await client.PostAsync(_endpoint, data);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseBody);

            return json["translatedText"].ToString();
        }
    }
}
