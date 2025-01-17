using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class Translator
{
    private readonly string _baseUrl = "http://localhost:5000"; // Проверьте, что сервер работает

    public async Task<string> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "en")
    {
        using (var httpClient = new HttpClient())
        {
            var requestBody = new
            {
                q = text,
                source = sourceLanguage,
                target = targetLanguage,
                format = "text"
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{_baseUrl}/translate", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error during translation: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(jsonResponse);
            return result.translatedText;
        }
    }
}

