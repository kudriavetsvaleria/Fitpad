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
                var getRequest = new GetRequest("https://newsapi.org/v2/top-headlines?category=sports&apiKey=74c6a3de96d649e89ed0f00bcd3d5174");
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
