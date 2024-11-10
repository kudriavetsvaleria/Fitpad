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
                    string apiKey = "74c6a3de96d649e89ed0f00bcd3d5174";
                    string urlWithApiKey = $"{_address}&apiKey={apiKey}";


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
