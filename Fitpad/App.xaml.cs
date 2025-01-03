using System.Windows;
using Fitpad.View.Pages;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Создаем главное окно вручную
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel() // Устанавливаем DataContext
            };

     
        }
    }
}
