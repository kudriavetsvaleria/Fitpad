using System.Windows.Controls;
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad.View.Pages
{
    public partial class ProfilePage : Page
    {
        public ProfilePage(ProfileViewModel profileViewModel)
        {
            InitializeComponent();
            DataContext = profileViewModel; // Устанавливаем DataContext
        }
    }
}
