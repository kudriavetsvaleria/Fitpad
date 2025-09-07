using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using LiveCharts;
using LiveCharts.Wpf;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class CalculateNutritionViewModel : INotifyPropertyChanged
    {
        private readonly CalculateNutritionRepository _repository = new CalculateNutritionRepository();

        private double _currentCalories;
        private double _calorieNorm;

        private double _currentProtein, _currentFats, _currentCarbs, _currentWater;
        private double _proteinNorm, _fatsNorm, _carbsNorm, _waterNorm;

        public Action<string, double> ShowManualEntryOverlayAction { get; set; }

        public UserInfoModel UserInfo { get; }
        public ObservableCollection<NutritionModel> SavedProducts { get; }

        public SeriesCollection CalorieChartSeries { get; private set; }
        public SeriesCollection WaterChartSeries { get; private set; }

        private bool _canSave;
        public bool CanSave
        {
            get => _canSave;
            set { _canSave = value; OnPropertyChanged(); }
        }

        public CalculateNutritionViewModel(UserInfoModel userInfo)
        {
            UserInfo = userInfo ?? new UserInfoModel();
            SavedProducts = new ObservableCollection<NutritionModel>();

            CalorieNorm = CalculateDailyCalorieIntake(UserInfo);
            CalculateMacroNorms();

            // Инициализация серий (значения будут перезаписаны в UpdatePieChart)
            CalorieChartSeries = new SeriesCollection
            {
                new PieSeries { Title = "З'їли",      Values = new ChartValues<double> { 0 },         Fill = new SolidColorBrush(Colors.Green), DataLabels = true },
                new PieSeries { Title = "Залишилось", Values = new ChartValues<double> { CalorieNorm }, Fill = new SolidColorBrush(Colors.Gray),  DataLabels = true }
            };

            WaterChartSeries = new SeriesCollection
            {
                new PieSeries { Title = "Випито",     Values = new ChartValues<double> { 0 },        Fill = new SolidColorBrush(Color.FromRgb(122,206,255)), DataLabels = true },
                new PieSeries { Title = "Залишилось", Values = new ChartValues<double> { WaterNorm }, Fill = new SolidColorBrush(Color.FromRgb(190,190,190)),  DataLabels = true }
            };

            UpdatePieChart();
        }

        // опциональный конструктор, если где-то создаётся по userId
        public CalculateNutritionViewModel(string userId) : this(new UserInfoModel { UserId = userId }) { }

        // ----- Валидация ввода
        public void CheckFields(string calories, string protein, string fats, string carbs)
        {
            CanSave = !string.IsNullOrWhiteSpace(calories)
                   && !string.IsNullOrWhiteSpace(protein)
                   && !string.IsNullOrWhiteSpace(fats)
                   && !string.IsNullOrWhiteSpace(carbs);
        }

        // ----- Текущие значения
        public double CurrentCalories
        {
            get => _currentCalories;
            set { _currentCalories = value; OnPropertyChanged(nameof(CalorieDisplayText)); }
        }
        public double CurrentProtein { get => _currentProtein; set { _currentProtein = value; OnPropertyChanged(nameof(ProteinDisplayText)); } }
        public double CurrentFats { get => _currentFats; set { _currentFats = value; OnPropertyChanged(nameof(FatsDisplayText)); } }
        public double CurrentCarbs { get => _currentCarbs; set { _currentCarbs = value; OnPropertyChanged(nameof(CarbsDisplayText)); } }
        public double CurrentWater { get => _currentWater; set { _currentWater = value; OnPropertyChanged(nameof(WaterDisplayText)); } }

        // ----- Нормы
        public double CalorieNorm
        {
            get => _calorieNorm;
            set
            {
                _calorieNorm = value;
                CalculateMacroNorms();
                OnPropertyChanged(nameof(CalorieDisplayText));
                OnPropertyChanged(nameof(ProteinDisplayText));
                OnPropertyChanged(nameof(FatsDisplayText));
                OnPropertyChanged(nameof(CarbsDisplayText));
            }
        }
        public double ProteinNorm { get => _proteinNorm; set { _proteinNorm = value; OnPropertyChanged(nameof(ProteinDisplayText)); } }
        public double FatsNorm { get => _fatsNorm; set { _fatsNorm = value; OnPropertyChanged(nameof(FatsDisplayText)); } }
        public double CarbsNorm { get => _carbsNorm; set { _carbsNorm = value; OnPropertyChanged(nameof(CarbsDisplayText)); } }
        public double WaterNorm { get => _waterNorm; set { _waterNorm = value; OnPropertyChanged(nameof(WaterDisplayText)); } }

        public string CalorieDisplayText => $"Ккал: {CurrentCalories:F1} / {CalorieNorm:F0}";
        public string ProteinDisplayText => $"{CurrentProtein:F1} / {ProteinNorm:F0} г";
        public string FatsDisplayText => $"{CurrentFats:F1} / {FatsNorm:F0} г";
        public string CarbsDisplayText => $"{CurrentCarbs:F1} / {CarbsNorm:F0} г";
        public string WaterDisplayText => $"{CurrentWater:F1} / {WaterNorm:F0} мл";

        private void CalculateMacroNorms()
        {
            if (CalorieNorm <= 0) { ProteinNorm = FatsNorm = CarbsNorm = 0; WaterNorm = 2000; return; }

            ProteinNorm = Math.Round((CalorieNorm * 0.20) / 4, 1);
            FatsNorm = Math.Round((CalorieNorm * 0.25) / 9, 1);
            CarbsNorm = Math.Round((CalorieNorm * 0.55) / 4, 1);
            WaterNorm = 2000; // мл

            OnPropertyChanged(nameof(ProteinNorm));
            OnPropertyChanged(nameof(FatsNorm));
            OnPropertyChanged(nameof(CarbsNorm));
            OnPropertyChanged(nameof(WaterNorm));
            OnPropertyChanged(nameof(WaterDisplayText));
        }

        public void UpdatePieChart()
        {
            // защита от деления на ноль
            var calPercent = CalorieNorm > 0 ? (CurrentCalories / CalorieNorm) * 100 : 0;
            var waterPct = WaterNorm > 0 ? (CurrentWater / WaterNorm) * 100 : 0;

            calPercent = Math.Max(0, Math.Min(100, calPercent));
            waterPct = Math.Max(0, Math.Min(100, waterPct));

            Application.Current.Dispatcher.Invoke(() =>
            {
                var green = (Brush)new BrushConverter().ConvertFromString("#70A93D");
                var gray = (Brush)new BrushConverter().ConvertFromString("#BEBEBE");

                CalorieChartSeries = new SeriesCollection
                {
                    new PieSeries { Title = "З'їли",      Values = new ChartValues<double> { Math.Round(calPercent, 2) },      Fill = green, DataLabels = true, LabelPoint = cp => $"{cp.Y:F2}%" },
                    new PieSeries { Title = "Залишилось", Values = new ChartValues<double> { Math.Round(100 - calPercent, 2) }, Fill = gray,  DataLabels = true, LabelPoint = cp => $"{cp.Y:F2}%" }
                };
                OnPropertyChanged(nameof(CalorieChartSeries));

                WaterChartSeries = new SeriesCollection
                {
                    new PieSeries { Title = "Випито",     Values = new ChartValues<double> { Math.Round(waterPct, 2) },        Fill = new SolidColorBrush(Color.FromRgb(122,206,255)), DataLabels = true, LabelPoint = cp => $"{cp.Y:F2}%" },
                    new PieSeries { Title = "Залишилось", Values = new ChartValues<double> { Math.Round(100 - waterPct, 2) },  Fill = gray,                                        DataLabels = true, LabelPoint = cp => $"{cp.Y:F2}%" }
                };
                OnPropertyChanged(nameof(WaterChartSeries));
            });
        }

        public void AddProduct(NutritionModel product)
        {
            if (product == null) return;

            CurrentCalories += product.Calories;
            CurrentProtein += product.Protein;
            CurrentFats += product.Fats;
            CurrentCarbs += product.Carbs;

            if ((product.Title ?? "").Equals("вода", StringComparison.OrdinalIgnoreCase) ||
                (product.Title ?? "").Equals("water", StringComparison.OrdinalIgnoreCase))
            {
                CurrentWater += product.Weight;
            }

            UpdatePieChart();
        }

        public async Task<NutritionModel> SearchAndAddProductAsync(string query, double weight)
        {
            if (weight <= 0) throw new ArgumentException("Вага повинна бути більше 0 г.");

            var lower = (query ?? "").Trim().ToLowerInvariant();
            if (lower == "вода" || lower == "water")
            {
                return new NutritionModel
                {
                    Title = "Вода",
                    Weight = weight,
                    Calories = 0,
                    Protein = 0,
                    Fats = 0,
                    Carbs = 0,
                    Water = weight,
                    Time = DateTime.Now.ToString("HH:mm")
                };
            }

            // ВНИМАНИЕ: сюда уже лучше подавать переведённый на EN текст
            var products100g = await _repository.GetProductsAsync(query);
            if (products100g == null || products100g.Count == 0) return null;

            var p100 = products100g[0];

            return new NutritionModel
            {
                Title = p100.Title,
                Name = p100.Name,
                Image = p100.Image,
                Weight = weight,
                Time = DateTime.Now.ToString("HH:mm"),

                Calories = (p100.Calories * weight) / 100.0,
                Protein = (p100.Protein * weight) / 100.0,
                Fats = (p100.Fats * weight) / 100.0,
                Carbs = (p100.Carbs * weight) / 100.0,
                Sugar = (p100.Sugar * weight) / 100.0,
                Water = 0
            };
        }

        public double CalculateDailyCalorieIntake(UserInfoModel userInfo)
        {
            if (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0) return 0;

            double bmr = (userInfo.Gender == "Чоловік" || userInfo.Gender == "Мужчина")
                ? 88.36 + (13.4 * userInfo.Weight) + (4.8 * userInfo.Height) - (5.7 * userInfo.Age)
                : 447.6 + (9.2 * userInfo.Weight) + (3.1 * userInfo.Height) - (4.3 * userInfo.Age);

            double activity = userInfo.ActivityLevel switch
            {
                "Низька" => 1.2,
                "Середня" => 1.375,
                "Висока" => 1.55,
                "Дуже висока" => 1.725,
                "Екстремальна" => 1.9,
                _ => 1.2
            };

            double tdee = bmr * activity;
            tdee = userInfo.Purpose switch
            {
                "Схуднення" => tdee * 0.85,
                "Набір маси" => tdee * 1.15,
                _ => tdee
            };

            return Math.Round(tdee);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
