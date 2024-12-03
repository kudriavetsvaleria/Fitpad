using Fitpad.ViewModel.PagesViewModels;
using System.Windows.Controls;

namespace Fitpad.View.Pages
{
    public partial class NutritionPage : Page
    {
        public NutritionPage()
        {
            InitializeComponent();
            DataContext = new NutritionViewModel();
        }
    }
}
