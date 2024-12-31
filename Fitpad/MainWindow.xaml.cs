using Fitpad.Model.Entities;
using System.Windows;

namespace Fitpad
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
