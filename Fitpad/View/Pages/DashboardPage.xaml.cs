using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.Generic;
using System.Linq;
using Fitpad.View.Components;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fitpad.Model.Entities;

namespace Fitpad.View.Pages
{
    public partial class DashboardPage : Page
    {
        private static DashboardPage _instance;
        private static readonly object _lock = new object();

        // Универсальная версия — можно передавать UserModel или ViewModel
        public static DashboardPage GetInstance(object context = null)
        {
            lock (_lock)
            {
                if (_instance == null)
                    _instance = new DashboardPage(context);
                return _instance;
            }
        }

        public static void ResetInstance()
        {
            lock (_lock) { _instance = null; }
        }

        private readonly DashboardViewModel _vm;

        public DashboardPage(object context = null)
        {
            InitializeComponent();

            if (context is DashboardViewModel vm)
                _vm = vm;
            else if (context is UserModel user)
                _vm = new DashboardViewModel(user);
            else
                _vm = new DashboardViewModel(null);

            DataContext = _vm;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var userId = _vm.CurrentUser != null ? _vm.CurrentUser.Id : null;
            if (string.IsNullOrWhiteSpace(userId))
                return;

            await _vm.LoadDashboardDataAsync(userId);

            CaloriesChart.Series = _vm.CaloriesChartSeries;
            SetupCaloriesAxes();
            MacroChart.Series = _vm.MacroChartSeries;

            ApplyKpiTexts();
            ApplyMiniDashboardTexts();
            ApplyGeneralStatsTexts();
        }

        private void SetupCaloriesAxes()
        {
            CaloriesChart.AxisX.Clear();
            CaloriesChart.AxisY.Clear();

            CaloriesChart.AxisX.Add(new Axis
            {
                Labels = _vm.CaloriesAxisLabels != null ? _vm.CaloriesAxisLabels.ToList() : new List<string>(),
                Title = "Дата",
                FontSize = 11
            });

            CaloriesChart.AxisY.Add(new Axis
            {
                Title = "Ккал",
                LabelFormatter = val => val.ToString("N0")
            });
        }

        private void ApplyKpiTexts()
        {
            var rootDock = FindVisualChildren<DockPanel>(this).FirstOrDefault();
            if (rootDock == null) return;

            var leftStack = rootDock.Children.OfType<StackPanel>().FirstOrDefault();
            if (leftStack == null) return;

            var cards = leftStack.Children.OfType<Border>().ToList();
            if (cards.Count < 4) return;

            SetCardTexts(cards[0], null, _vm.KpiCaloriesToday, _vm.KpiCaloriesDeltaText);
            // Баланс БЖВ
            SetCardTexts(cards[1], null, _vm.KpiMacroBalanceText, _vm.KpiMacroDeltaText);
            SetCardTexts(cards[2], null, _vm.KpiProgressToGoal, _vm.KpiProgressDeltaText);
            SetCardTexts(cards[3], null, _vm.KpiWaterToday, _vm.KpiWaterDeltaText);
        }

        private void ApplyMiniDashboardTexts()
        {
            var rootDock = FindVisualChildren<DockPanel>(this).FirstOrDefault();
            if (rootDock == null) return;

            var rightBorder = rootDock.Children.OfType<Border>().LastOrDefault();
            if (rightBorder == null) return;

            var stack = FindVisualChildren<StackPanel>(rightBorder).FirstOrDefault();
            if (stack == null) return;

            var tbs = stack.Children.OfType<TextBlock>().ToList();
            if (tbs.Count >= 1) tbs[0].Text = _vm.MiniDashboard_Name ?? "";
            if (tbs.Count >= 2) tbs[1].Text = _vm.MiniDashboard_Level ?? "";
        }

        private void ApplyGeneralStatsTexts()
        {
            var allTB = FindVisualChildren<TextBlock>(this).ToList();
            var header = allTB.FirstOrDefault(t => (t.Text ?? "").Trim().StartsWith("Загальна статистика"));
            if (header == null) return;

            var container = FindAncestor<StackPanel>(header);
            if (container == null) return;

            var rows = container.Children.OfType<StackPanel>()
                .Where(sp => sp.Orientation == Orientation.Horizontal)
                .ToList();

            if (rows.Count >= 1)
            {
                var row1 = rows[0].Children.OfType<TextBlock>().LastOrDefault();
                if (row1 != null) row1.Text = _vm.StatDaysActiveText;
            }
            if (rows.Count >= 2)
            {
                var row2 = rows[1].Children.OfType<TextBlock>().LastOrDefault();
                if (row2 != null) row2.Text = _vm.StatSavedDishesText;
            }
            if (rows.Count >= 3)
            {
                var row3 = rows[2].Children.OfType<TextBlock>().LastOrDefault();
                if (row3 != null) row3.Text = _vm.StatProductsInBaseText;
            }
        }

        private static void SetCardTexts(Border card, string header, string main, string sub)
        {
            if (card != null && card.Child is StackPanel sp)
            {
                var tbs = sp.Children.OfType<TextBlock>().ToList();
                if (tbs.Count >= 1 && header != null) tbs[0].Text = header;
                if (tbs.Count >= 2) tbs[1].Text = main ?? "";
                if (tbs.Count >= 3) tbs[2].Text = sub ?? "";
            }
        }

        private static T FindAncestor<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
                parent = VisualTreeHelper.GetParent(parent);
            return parent as T;
        }

        private void MiniDashboard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var window = new UserInfoWindow(_vm);
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }


        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T)
                    yield return (T)child;

                foreach (var c in FindVisualChildren<T>(child))
                    yield return c;
            }
        }
    }
}
