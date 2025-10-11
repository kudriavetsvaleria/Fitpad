using Fitpad.Services;
using Fitpad.ViewModel.PagesViewModels;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
// ✅ просто пространство имён, где лежит CalculateNutritionViewModel
using Fitpad.ViewModel.PagesViewModels;

namespace Fitpad.View.Pages
{
    public partial class ProfilePage : Page
    {
        private readonly FirestoreService _firestoreService;
        private readonly ProfileViewModel _profileViewModel;
        private bool _profileExpanded = false;
        private int _daysRange = 7;

        public ProfilePage(ProfileViewModel profileViewModel)
        {
            InitializeComponent();
            _firestoreService = new FirestoreService();
            _profileViewModel = profileViewModel;
            DataContext = profileViewModel;

            Loaded += async (s, e) =>
            {
                await LoadCalorieChartAsync();
                await LoadMacroChartAsync();
            };
        }

        private void ProfileCard_Click(object sender, MouseButtonEventArgs e)
        {
            _profileExpanded = !_profileExpanded;
            ProfileButtons.Visibility = _profileExpanded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Редагування профілю поки не реалізовано, але тут відкриється форма редагування.",
                            "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance?.Logout();
        }

        private void Range_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || _firestoreService == null || _profileViewModel == null)
                return;

            if (sender is RadioButton rb && int.TryParse(rb.Tag?.ToString(), out int days))
            {
                _daysRange = days;
                _ = LoadCalorieChartAsync();
            }
        }

        // ===================== ГРАФІК КАЛОРІЙ =====================
        private async Task LoadCalorieChartAsync()
        {
            string userId = _profileViewModel.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return;

            DateTime today = DateTime.Now.Date;
            DateTime from = today.AddDays(-_daysRange + 1);

            var summaries = await _firestoreService.GetDaySummariesAsync(userId, from, today);
            if (summaries == null || summaries.Count == 0) return;

            summaries = summaries.OrderBy(s => s.Date).ToList();

            var labels = summaries.Select(s => s.Date.Substring(5)).ToArray();
            var calories = summaries.Select(s => s.Calories).ToArray();
            var protein = summaries.Select(s => s.Protein).ToArray();
            var fats = summaries.Select(s => s.Fats).ToArray();
            var carbs = summaries.Select(s => s.Carbs).ToArray();
            var water = summaries.Select(s => s.Water).ToArray();

            // 🔹 вычисляем норму калорій пользователя
            double calorieNorm = 0;
            if (_profileViewModel.CurrentUserInfo != null)
            {
                var calcVM = new CalculateNutritionViewModel(_profileViewModel.CurrentUserInfo);
                calorieNorm = calcVM.CalculateDailyCalorieIntake(_profileViewModel.CurrentUserInfo);
            }

            var normValues = Enumerable.Repeat(calorieNorm, summaries.Count).ToArray();

            CaloriesChart.Series = new SeriesCollection
            {
                // 🔹 зона нормы (серая полоса)
                new LineSeries
                {
                    Title = "Норма калорій",
                    Values = new ChartValues<double>(normValues),
                    Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    Fill = new SolidColorBrush(Color.FromArgb(40, 100, 100, 100)),
                    PointGeometry = null,
                    LineSmoothness = 0,
                    StrokeThickness = 2
                },
                new LineSeries { Title="Калорії", Values=new ChartValues<double>(calories), Stroke=Brushes.Black, Fill=Brushes.Transparent, PointGeometrySize=6 },
                new LineSeries { Title="Білки", Values=new ChartValues<double>(protein), Stroke=Brushes.Blue, Fill=Brushes.Transparent, PointGeometrySize=6 },
                new LineSeries { Title="Жири", Values=new ChartValues<double>(fats), Stroke=Brushes.Red, Fill=Brushes.Transparent, PointGeometrySize=6 },
                new LineSeries { Title="Вуглеводи", Values=new ChartValues<double>(carbs), Stroke=Brushes.Green, Fill=Brushes.Transparent, PointGeometrySize=6 },
                new LineSeries { Title="Вода", Values=new ChartValues<double>(water), Stroke=Brushes.SkyBlue, Fill=Brushes.Transparent, PointGeometrySize=6 }
            };

            CaloriesChart.AxisX.Clear();
            CaloriesChart.AxisY.Clear();

            CaloriesChart.AxisX.Add(new Axis
            {
                Labels = labels,
                Title = "Дата",
                FontSize = 12,
                Foreground = Brushes.Gray
            });

            CaloriesChart.AxisY.Add(new Axis
            {
                Title = "Ккал / г / мл",
                FontSize = 12,
                Foreground = Brushes.Gray
            });
        }

        // ===================== ДІАГРАМА БЖВ =====================
        private async Task LoadMacroChartAsync()
        {
            string userId = _profileViewModel.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return;

            var today = DateTime.Now.Date;
            var summary = await _firestoreService.GetDaySummaryAsync(userId, today);
            if (summary == null) return;

            MacroBalanceChart.Series = new SeriesCollection
            {
                new PieSeries { Title="Білки", Values=new ChartValues<double>{summary.Protein}, Fill=Brushes.Blue },
                new PieSeries { Title="Жири", Values=new ChartValues<double>{summary.Fats}, Fill=Brushes.Red },
                new PieSeries { Title="Вуглеводи", Values=new ChartValues<double>{summary.Carbs}, Fill=Brushes.Green }
            };

            MacroBalanceChart.LegendLocation = LegendLocation.Right;
        }

        // ===================== SINGLETON =====================
        private static ProfilePage _instance;
        private static readonly object _lock = new object();

        public static ProfilePage GetInstance(ProfileViewModel profileViewModel = null)
        {
            lock (_lock)
            {
                if (_instance == null || profileViewModel != null)
                    _instance = new ProfilePage(profileViewModel ?? new ProfileViewModel());
                return _instance;
            }
        }

        public static void ResetInstance()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }
    }
}
