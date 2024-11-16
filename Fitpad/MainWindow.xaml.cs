using Fitpad.View.Repositories;
using System.Windows;
using System;

namespace Fitpad
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            RunRequest();
        }

        private async void RunRequest()
        {
            try
            {
                var getRequest = new GetRequest("https://newsapi.org/v2/top-headlines?category=sports&apiKey=6be473200c65428498902906f4d6f1b4");
                await getRequest.RunAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error during request: {ex.Message}");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
