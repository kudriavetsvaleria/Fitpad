using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fitpad.View.Repositories
{
    public class GetRequest
    {
        string _address;
        public string Response { get; set; }

        public GetRequest(string address)
        {
            _address = address;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("=========--- RunAsync ---==========");
            using (var client = new HttpClient())
            {
             
                try
                {
                    string urlWithApiKey = Uri.EscapeUriString($"https://newsapi.org/v2/top-headlines?category=sports&apiKey=6be473200c65428498902906f4d6f1b4");



                    // Отправляем GET-запрос и получаем ответ в виде строки
                    Response = await client.GetStringAsync(urlWithApiKey);
                    Console.WriteLine("============================ API data from parser: ============================");
                    Console.WriteLine(Response); // Выводим результат в консоль (или используйте его дальше)
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        }
    }
}
