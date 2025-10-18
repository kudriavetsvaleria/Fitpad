using Fitpad.Model.Entities;
using Fitpad.Services;
using Google.Cloud.Firestore;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Fitpad.View.Pages
{
    public class ProductRow
    {
        public string Name { get; set; }
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbs { get; set; }
    }

    public class ProfileViewModel : INotifyPropertyChanged
    {
        private readonly FirestoreService _fs;

        // -------- User / UserInfo ----------
        private UserModel _currentUser;
        private UserInfoModel _currentUserInfo;

        public UserModel CurrentUser
        {
            get => _currentUser;
            set { _currentUser = value; OnPropertyChanged(); }
        }
        public UserInfoModel CurrentUserInfo
        {
            get => _currentUserInfo;
            set { _currentUserInfo = value; OnPropertyChanged(); }
        }

        // -------- KPI / Profile texts (для существующих TextBlock) ----------
        public string KpiCaloriesToday { get => _kpiCaloriesToday; private set { _kpiCaloriesToday = value; OnPropertyChanged(); } }
        private string _kpiCaloriesToday = "";

        public string KpiMacroDeltaText
        {
            get => _kpiMacroDeltaText;
            private set { _kpiMacroDeltaText = value; OnPropertyChanged(); }
        }
        private string _kpiMacroDeltaText = "";

        public string KpiCaloriesDeltaText { get => _kpiCaloriesDeltaText; private set { _kpiCaloriesDeltaText = value; OnPropertyChanged(); } }
        private string _kpiCaloriesDeltaText = "";

        public string KpiMacroBalanceText { get => _kpiMacroBalanceText; private set { _kpiMacroBalanceText = value; OnPropertyChanged(); } }
        private string _kpiMacroBalanceText = "";

        public string KpiProgressToGoal { get => _kpiProgressToGoal; private set { _kpiProgressToGoal = value; OnPropertyChanged(); } }
        private string _kpiProgressToGoal = "";

        public string KpiProgressDeltaText { get => _kpiProgressDeltaText; private set { _kpiProgressDeltaText = value; OnPropertyChanged(); } }
        private string _kpiProgressDeltaText = "";

        public string KpiWaterToday { get => _kpiWaterToday; private set { _kpiWaterToday = value; OnPropertyChanged(); } }
        private string _kpiWaterToday = "";

        public string KpiWaterDeltaText { get => _kpiWaterDeltaText; private set { _kpiWaterDeltaText = value; OnPropertyChanged(); } }
        private string _kpiWaterDeltaText = "";

        public string MiniProfile_Name { get => _miniProfile_Name; private set { _miniProfile_Name = value; OnPropertyChanged(); } }
        private string _miniProfile_Name = "";

        public string MiniProfile_Level { get => _miniProfile_Level; private set { _miniProfile_Level = value; OnPropertyChanged(); } }
        private string _miniProfile_Level = "";

        // -------- General stats texts ----------
        public string StatDaysActiveText { get => _statDaysActiveText; private set { _statDaysActiveText = value; OnPropertyChanged(); } }
        private string _statDaysActiveText = "";

        public string StatSavedDishesText { get => _statSavedDishesText; private set { _statSavedDishesText = value; OnPropertyChanged(); } }
        private string _statSavedDishesText = "";

        public string StatProductsInBaseText { get => _statProductsInBaseText; private set { _statProductsInBaseText = value; OnPropertyChanged(); } }
        private string _statProductsInBaseText = "";

        // -------- LiveCharts: calories line  ----------
        public SeriesCollection CaloriesChartSeries { get => _caloriesChartSeries; private set { _caloriesChartSeries = value; OnPropertyChanged(); } }
        private SeriesCollection _caloriesChartSeries = new SeriesCollection();

        public IList<string> CaloriesAxisLabels { get => _caloriesAxisLabels; private set { _caloriesAxisLabels = value; OnPropertyChanged(); } }
        private IList<string> _caloriesAxisLabels = new List<string>();

        // -------- LiveCharts: macro pie ----------
        public SeriesCollection MacroChartSeries { get => _macroChartSeries; private set { _macroChartSeries = value; OnPropertyChanged(); } }
        private SeriesCollection _macroChartSeries = new SeriesCollection();

        // -------- Table ----------
        public ObservableCollection<ProductRow> FrequentProducts { get; } = new ObservableCollection<ProductRow>();

        public ProfileViewModel(UserModel user = null)
        {
            _fs = new FirestoreService();
            CurrentUser = user;
        }

        // ======== PUBLIC ENTRY =========
        public async Task LoadDashboardDataAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            // User & profile
            await LoadUserAndInfoAsync(userId);

            // Charts
            await LoadWeeklyCaloriesChartAsync(userId);
            await LoadMacroChartAsync(userId);

            // Table
            await LoadFrequentProductsAsync(userId);

            // General stats & KPI cards
            await LoadGeneralStatsAsync(userId);
            await BuildKpiTextsAsync(userId);
        }

        // ---------- User & UserInfo ----------
        private async Task LoadUserAndInfoAsync(string userId)
        {
            // Если у тебя есть Users/{id} документ — подгрузить имя/почту.
            // (У тебя уже есть SaveUserAsync, но явного GetUser нет. Пропускаем получение UserModel и работаем с UserInfo.)
            CurrentUserInfo = await _fs.GetUserInfoAsync(userId);

            MiniProfile_Name = !string.IsNullOrWhiteSpace(CurrentUser?.Name)
                ? CurrentUser.Name
                : (CurrentUserInfo != null ? (CurrentUserInfo.UserId ?? "User") : "User");

            // "Рівень" у схемі явно не зберігається, тому показуємо пусто/0
            MiniProfile_Level = ""; // якщо з’явиться поле Level — легко підставимо сюди
        }

        // ---------- Weekly calories chart ----------
        // ---------- Weekly calories chart ----------
        private async Task LoadWeeklyCaloriesChartAsync(string userId)
        {
            var to = DateTime.Now.Date;
            var from = to.AddDays(-6);

            var summaries = await _fs.GetDaySummariesAsync(userId, from, to);

            var tdee = EstimateTDEE(CurrentUserInfo);
            if (tdee <= 0) tdee = 2000;

            var labels = new List<string>();
            var normValues = new ChartValues<double>();
            var actualCalories = new ChartValues<double>();
            var proteins = new ChartValues<double>();
            var fats = new ChartValues<double>();
            var carbs = new ChartValues<double>();

            for (int i = 0; i < 7; i++)
            {
                var d = from.AddDays(i);
                labels.Add(d.ToString("MM-dd"));
                var s = summaries.FirstOrDefault(x => x.Date == d.ToString("yyyy-MM-dd"));
                normValues.Add(tdee);
                actualCalories.Add(s?.Calories ?? 0);
                proteins.Add(s?.Protein ?? 0);
                fats.Add(s?.Fats ?? 0);
                carbs.Add(s?.Carbs ?? 0);
            }

            CaloriesAxisLabels = labels;

            CaloriesChartSeries = new SeriesCollection
    {
        new LineSeries
        {
            Title = "Норма",
            Values = normValues,
            Stroke = Brushes.Gray,
            Fill = Brushes.Transparent,
            PointGeometry = null,
            StrokeThickness = 2
        },
        new LineSeries
        {
            Title = "Калорії",
            Values = actualCalories,
            Stroke = new SolidColorBrush(Color.FromRgb(108, 99, 255)),
            Fill = new SolidColorBrush(Color.FromArgb(40, 108, 99, 255)),
            StrokeThickness = 3,
            PointGeometrySize = 5
        },
        new LineSeries
        {
            Title = "Білки",
            Values = proteins,
            Stroke = Brushes.Blue,
            Fill = Brushes.Transparent,
            PointGeometrySize = 4,
            StrokeThickness = 2
        },
        new LineSeries
        {
            Title = "Жири",
            Values = fats,
            Stroke = Brushes.Red,
            Fill = Brushes.Transparent,
            PointGeometrySize = 4,
            StrokeThickness = 2
        },
        new LineSeries
        {
            Title = "Вуглеводи",
            Values = carbs,
            Stroke = Brushes.Green,
            Fill = Brushes.Transparent,
            PointGeometrySize = 4,
            StrokeThickness = 2
        }
    };
        }

        // ---------- Macro donut chart (today) ----------
        private async Task LoadMacroChartAsync(string userId)
        {
            var today = DateTime.Now.Date;
            var s = await _fs.GetDaySummaryAsync(userId, today) ?? new DaySummaryModel();

            var p = Math.Max(0, s.Protein);
            var f = Math.Max(0, s.Fats);
            var c = Math.Max(0, s.Carbs);

            MacroChartSeries = new SeriesCollection
            {
                new PieSeries { Title = "Білки",      Values = new ChartValues<double> { p }, Fill = Brushes.Blue,  PushOut = 2 },
                new PieSeries { Title = "Жири",       Values = new ChartValues<double> { f }, Fill = Brushes.Red,   PushOut = 2 },
                new PieSeries { Title = "Вуглеводи",  Values = new ChartValues<double> { c }, Fill = Brushes.Green, PushOut = 2 }
            };
        }

        // ---------- Frequent products table ----------
        private async Task LoadFrequentProductsAsync(string userId)
        {
            FrequentProducts.Clear();

            // Собираем потребление за последние 14 дней и агрегируем по Title
            var from = DateTime.Now.Date.AddDays(-13);
            var dict = new Dictionary<string, (double kcal, double p, double f, double c)>();

            for (int i = 0; i < 14; i++)
            {
                var day = from.AddDays(i);
                var diary = await _fs.GetFoodDiaryForDateAsync(userId, day);
                foreach (var item in diary)
                {
                    var key = string.IsNullOrWhiteSpace(item.Title) ? "—" : item.Title.Trim();
                    if (!dict.ContainsKey(key)) dict[key] = (0, 0, 0, 0);
                    var v = dict[key];
                    v.kcal += item.Calories;
                    v.p += item.Protein;
                    v.f += item.Fats;
                    v.c += item.Carbs;
                    dict[key] = v;
                }
            }

            foreach (var kv in dict
                     .OrderByDescending(kv => kv.Value.kcal)
                     .Take(15))
            {
                FrequentProducts.Add(new ProductRow
                {
                    Name = kv.Key,
                    Calories = Math.Round(kv.Value.kcal, 0),
                    Protein = Math.Round(kv.Value.p, 1),
                    Fat = Math.Round(kv.Value.f, 1),
                    Carbs = Math.Round(kv.Value.c, 1)
                });
            }
        }

        // ---------- General stats & KPI ----------
        private async Task LoadGeneralStatsAsync(string userId)
        {
            // Дні активності: беремо за останні 90 днів
            var to = DateTime.Now.Date;
            var from = to.AddDays(-89);
            var sums = await _fs.GetDaySummariesAsync(userId, from, to);
            var activeDays = sums.Count(s => (s?.ItemsCount ?? 0) > 0);

            // Кількість страв
            var dishes = await _fs.GetUserDishesAsync(userId);
            var dishesCount = dishes?.Count ?? 0;

            // Кількість продуктів у каталозі користувача
            // Через FirestoreDb напряму (UserProducts) — тільки для підрахунку
            int productsCount = 0;
            try
            {
                var db = _fs.GetFirestoreDb();
                var snap = await db.Collection("Users").Document(userId)
                    .Collection("UserProducts").Limit(1_000).GetSnapshotAsync();
                productsCount = snap?.Count ?? 0;
            }
            catch { /* ignore */ }

            StatDaysActiveText = $"Днів активності: {activeDays}";
            StatSavedDishesText = $"Збережених страв: {dishesCount}";
            StatProductsInBaseText = $"Продуктів у базі: {productsCount}";
        }

        private async Task BuildKpiTextsAsync(string userId)
        {
            var today = DateTime.Now.Date;
            var yesterday = today.AddDays(-1);

            // 🔹 добавлено: расчёт суточной нормы TDEE
            var tdee = EstimateTDEE(CurrentUserInfo);
            if (tdee <= 0) tdee = 2000;

            var sToday = await _fs.RecomputeDaySummaryAsync(userId, today) ?? new DaySummaryModel();
            var sYesterday = await _fs.GetDaySummaryAsync(userId, yesterday);

            // 🔸 Калорії сьогодні
            KpiCaloriesToday = sToday.Calories > 0 ? $"{Math.Round(sToday.Calories, 0):N0} ккал" : "";
            if (sYesterday != null && sYesterday.Calories > 0 && sToday.Calories > 0)
            {
                var delta = (sToday.Calories - sYesterday.Calories) / Math.Max(1, sYesterday.Calories) * 100.0;
                var sign = delta >= 0 ? "+" : "";
                KpiCaloriesDeltaText = $"{sign}{Math.Round(delta, 1)} % до вчора";
            }
            else
            {
                // если нет вчерашних данных — показываем % от TDEE
                if (tdee > 0 && sToday.Calories > 0)
                {
                    var percent = Math.Round(sToday.Calories / tdee * 100.0 - 100.0, 1);
                    var sign = percent >= 0 ? "+" : "";
                    KpiCaloriesDeltaText = $"{sign}{percent}% до мети";
                }
            }

            // 🔸 Баланс БЖВ

            // 🔸 Баланс БЖВ
            KpiMacroBalanceText =
                sToday.Protein + sToday.Fats + sToday.Carbs > 0
                ? $"{Math.Round(sToday.Protein, 0)} / {Math.Round(sToday.Fats, 0)} / {Math.Round(sToday.Carbs, 0)}"
                : "";

            if (sToday.Protein > 0 && sToday.Fats > 0 && sToday.Carbs > 0)
            {
                var ratio = sToday.Protein + sToday.Fats + sToday.Carbs;
                var percP = sToday.Protein / ratio * 100;
                var percF = sToday.Fats / ratio * 100;
                var percC = sToday.Carbs / ratio * 100;

                // идеальные диапазоны (±5%)
                bool goodP = percP >= 25 && percP <= 35;
                bool goodF = percF >= 20 && percF <= 30;
                bool goodC = percC >= 40 && percC <= 50;

                // по умолчанию просто проценты
                KpiMacroDeltaText = $"{Math.Round(percP)} / {Math.Round(percF)} / {Math.Round(percC)} %";

                if (goodP && goodF && goodC)
                {
                    KpiMacroDeltaText = "ідеальний баланс";
                }
                else
                {
                    // определяем, где перекос
                    if (percP < 25)
                        KpiMacroDeltaText = "варто додати білків";
                    else if (percF > 35)
                        KpiMacroDeltaText = "забагато жирів";
                    else if (percC > 55)
                        KpiMacroDeltaText = "занадто багато вуглеводів";
                    else if (percP > 40)
                        KpiMacroDeltaText = "забагато білків";
                    else if (percF < 15)
                        KpiMacroDeltaText = "недостатньо жирів";
                    else if (percC < 35)
                        KpiMacroDeltaText = "мало вуглеводів";
                    else
                        KpiMacroDeltaText = "дисбаланс БЖВ";
                }
            }
            else
            {
                KpiMacroDeltaText = "";
            }



            // 🔸 Прогрес до мети
            if (tdee > 0 && sToday.Calories > 0)
            {
                var progress = Math.Round(sToday.Calories / tdee * 100.0, 0);
                KpiProgressToGoal = $"{progress} %";
                var delta = progress - 100;
                var sign = delta >= 0 ? "+" : "";
                KpiProgressDeltaText = $"{sign}{Math.Round(delta, 1)} % за тиждень";
            }

            // 🔸 Вода сьогодні
            KpiWaterToday = sToday.Water > 0 ? $"{Math.Round(sToday.Water, 1)} л" : "";
            if (sYesterday != null && sYesterday.Water > 0 && sToday.Water > 0)
            {
                var deltaL = sToday.Water - sYesterday.Water;
                var sign = deltaL >= 0 ? "+" : "";
                KpiWaterDeltaText = $"{sign}{Math.Round(deltaL, 1)} л сьогодні";
            }
            else if (sToday.Water > 0)
            {
                KpiWaterDeltaText = "+0.0 л сьогодні";
            }
        }


        // ---------- Helpers ----------
        private static double EstimateTDEE(UserInfoModel info)
        {
            if (info == null) return 0;

            // Mifflin–St Jeor
            // пол: "male"/"female" (якщо інший — вважаємо female)
            var isMale = (info.Gender ?? "").Trim().ToLower().StartsWith("m");
            double bmr =
                isMale
                    ? 10 * info.Weight + 6.25 * info.Height - 5 * info.Age + 5
                    : 10 * info.Weight + 6.25 * info.Height - 5 * info.Age - 161;

            double activity = 1.2; // sedentary
            var lvl = (info.ActivityLevel ?? "").Trim().ToLower();
            if (lvl.Contains("light")) activity = 1.375;
            else if (lvl.Contains("moderate") || lvl.Contains("серед")) activity = 1.55;
            else if (lvl.Contains("high") || lvl.Contains("active") || lvl.Contains("вис")) activity = 1.725;
            else if (lvl.Contains("athlete") || lvl.Contains("дуже")) activity = 1.9;

            var tdee = bmr * activity;

            // Purpose: може бути "lose/maintain/gain" — легка корекціячета капец какой то( 
            var purpose = (info.Purpose ?? "").Trim().ToLower();
            if (purpose.Contains("lose") || purpose.Contains("схуд"))
                tdee *= 0.9;
            else if (purpose.Contains("gain") || purpose.Contains("набір"))
                tdee *= 1.1;

            return Math.Round(tdee, 0);
        }

        // ---------- INotifyPropertyChanged ----------
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
