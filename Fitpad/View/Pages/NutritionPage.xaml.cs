using System.Windows;
using Fitpad.ViewModel.PagesViewModels;
using System.Windows.Controls;
using Fitpad.Model.Entities;

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

            Loaded += async (s, e) => await _viewModel.LoadNutritionAsync();
        }

        private void Card_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is NutritionModel model)
            {
                NavigationService.Navigate(new RecipePage(model));
            }
        }
    }
}
