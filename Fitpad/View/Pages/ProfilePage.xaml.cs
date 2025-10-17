using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fitpad.View.Pages
{
    public partial class ProfilePage : Page
    {
        // === ЗАГЛУШКИ ДЛЯ ПАТТЕРНА SINGLETON ===
        private static ProfilePage _instance;
        private static readonly object _lock = new object();

        public static ProfilePage GetInstance(object profileViewModel = null)
        {
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new ProfilePage();
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
        // ========================================

        public ProfilePage()
        {
            InitializeComponent();
            LoadCharts();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) { }

        private void LoadCharts()
        {
            // 🔸 Линейный график (калории)
            var calories = new ChartValues<double> { 1800, 1950, 2100, 1900, 2300, 2250, 2400 };
            var norm = new ChartValues<double> { 2000, 2000, 2000, 2000, 2000, 2000, 2000 };

            CaloriesChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Норма",
                    Values = norm,
                    Stroke = Brushes.Gray,
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    StrokeThickness = 2
                },
                new LineSeries
                {
                    Title = "Фактичне споживання",
                    Values = calories,
                    Stroke = new SolidColorBrush(Color.FromRgb(108, 99, 255)),
                    Fill = new SolidColorBrush(Color.FromArgb(40, 108, 99, 255)),
                    StrokeThickness = 3,
                    PointGeometrySize = 6
                }
            };

            CaloriesChart.AxisX.Add(new Axis
            {
                Labels = new List<string> { "10-11", "10-12", "10-13", "10-14", "10-15", "10-16", "10-17" },
                Title = "Дата",
                FontSize = 11
            });

            CaloriesChart.AxisY.Add(new Axis
            {
                Title = "Ккал",
                LabelFormatter = val => val.ToString("N0")
            });

            // 🔸 Кольцевая диаграмма (БЖВ)
            MacroChart.Series = new SeriesCollection
            {
                new PieSeries { Title = "Білки", Values = new ChartValues<double> { 90 }, Fill = Brushes.Blue, PushOut = 2 },
                new PieSeries { Title = "Жири", Values = new ChartValues<double> { 60 }, Fill = Brushes.Red, PushOut = 2 },
                new PieSeries { Title = "Вуглеводи", Values = new ChartValues<double> { 200 }, Fill = Brushes.Green, PushOut = 2 }
            };


        }
    }
}
