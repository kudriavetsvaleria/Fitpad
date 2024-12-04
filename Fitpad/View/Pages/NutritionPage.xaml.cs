using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad.View.Pages
{
    public partial class NutritionPage : Page
    {
        private readonly NutritionViewModel _viewModel;

        public NutritionPage()
        {
            InitializeComponent();

            _viewModel = new NutritionViewModel();
            DataContext = _viewModel;

            // Асинхронная загрузка данных при входе на страницу
            Loaded += NutritionPage_Loaded;
        }

        private async void NutritionPage_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadNutritionAsync();
        }
    }
}
