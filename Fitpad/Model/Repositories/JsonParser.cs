using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fitpad.View.Repositories
{
    public class GetRequest
    {
        string _address;
        public string Response { get; set; }

        public GetRequest(string address){
            _address = address;
        }

        public async Task RunAsync()
        {
            using (var client = new HttpClient()) {
                try
                {
                    Response = await client.GetStringAsync(_address);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");

                }
            }
        }
    }
}
