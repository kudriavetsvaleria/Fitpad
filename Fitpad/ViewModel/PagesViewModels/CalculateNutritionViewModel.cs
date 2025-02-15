using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Fitpad.View.Components;
using LiveCharts;
using LiveCharts.Wpf;
using Fitpad.View;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class CalculateNutritionViewModel : INotifyPropertyChanged
    {
        private readonly CalculateNutritionRepository _repository;

        private readonly UserInfoRepository _userInfoRepository;
        private readonly string _userId;

        private double _currentCalories;
        private double _calorieNorm;

        private double _currentProtein, _currentFats, _currentCarbs, _currentWater;
        private double _proteinNorm, _fatsNorm, _carbsNorm, _waterNorm;

        public Action<string, double> ShowManualEntryOverlayAction { get; set; }
        public Action UpdatePieChartAction { get; set; }

        public UserInfoModel UserInfo { get; private set; }
        public ObservableCollection<NutritionModel> SavedProducts { get; set; }

        public SeriesCollection CalorieChartSeries { get; set; }
        public SeriesCollection WaterChartSeries { get; set; }

        public SeriesCollection ConsumedCaloriesSeries { get; set; }
        public SeriesCollection RemainingCaloriesSeries { get; set; }

        private bool _canSave;
        public bool CanSave
        {
            get => _canSave;
            set
            {
                _canSave = value;
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public CalculateNutritionViewModel(string userId)
        {
            _userId = userId;
            _userInfoRepository = new UserInfoRepository();
        }



        // Метод для проверки, заполнены ли все поля
        public void CheckFields(string calories, string protein, string fats, string carbs)
        {
            CanSave = !string.IsNullOrWhiteSpace(calories) &&
                      !string.IsNullOrWhiteSpace(protein) &&
                      !string.IsNullOrWhiteSpace(fats) &&
                      !string.IsNullOrWhiteSpace(carbs);
        }


        public double CurrentProtein
        {
            get => _currentProtein;
            set { _currentProtein = value; OnPropertyChanged(nameof(ProteinDisplayText)); }
        }
        public double CurrentFats
        {
            get => _currentFats;
            set { _currentFats = value; OnPropertyChanged(nameof(FatsDisplayText)); }
        }
        public double CurrentCarbs
        {
            get => _currentCarbs;
            set { _currentCarbs = value; OnPropertyChanged(nameof(CarbsDisplayText)); }
        }
        public double CurrentWater
        {
            get => _currentWater;
            set { _currentWater = value; OnPropertyChanged(nameof(WaterDisplayText)); }
        }

        public double ProteinNorm
        {
            get => _proteinNorm;
            set { _proteinNorm = value; OnPropertyChanged(nameof(ProteinDisplayText)); }
        }
        public double FatsNorm
        {
            get => _fatsNorm;
            set { _fatsNorm = value; OnPropertyChanged(nameof(FatsDisplayText)); }
        }
        public double CarbsNorm
        {
            get => _carbsNorm;
            set { _carbsNorm = value; OnPropertyChanged(nameof(CarbsDisplayText)); }
        }
        public double WaterNorm
        {
            get => _waterNorm;
            set { _waterNorm = value; OnPropertyChanged(nameof(WaterDisplayText)); }
        }

        public string ProteinDisplayText => $"{CurrentProtein:F1} / {ProteinNorm:F0} г";
        public string FatsDisplayText => $"{CurrentFats:F1} / {FatsNorm:F0} г";
        public string CarbsDisplayText => $"{CurrentCarbs:F1} / {CarbsNorm:F0} г";
        public string WaterDisplayText => $"{CurrentWater:F1} / {WaterNorm:F0} мл";



        private double _totalWater;


        public double CurrentCalories
        {
            get => _currentCalories;
            set
            {
                _currentCalories = value;
                UpdateCalorieText(); // ✅ Обновляем текстовое отображение калорий
            }
        }

        public double CalorieNorm
        {
            get => _calorieNorm;
            set
            {
                _calorieNorm = value;
                CalculateMacroNorms(); // ✅ Пересчитываем нормы БЖУ при изменении нормы калорий
                OnPropertyChanged(nameof(CalorieDisplayText));
                OnPropertyChanged(nameof(ProteinDisplayText));
                OnPropertyChanged(nameof(FatsDisplayText));
                OnPropertyChanged(nameof(CarbsDisplayText));
            }
        }

        private void CalculateMacroNorms()
        {
            if (CalorieNorm > 0) // Проверяем, чтобы не было деления на 0
            {
                ProteinNorm = Math.Round((CalorieNorm * 0.2) / 4, 1);
                FatsNorm = Math.Round((CalorieNorm * 0.25) / 9, 1);
                CarbsNorm = Math.Round((CalorieNorm * 0.55) / 4, 1);
                WaterNorm = 2000; // Минимальная суточная норма воды ✅

                OnPropertyChanged(nameof(ProteinNorm));
                OnPropertyChanged(nameof(FatsNorm));
                OnPropertyChanged(nameof(CarbsNorm));
                OnPropertyChanged(nameof(WaterNorm));
                OnPropertyChanged(nameof(WaterDisplayText)); // ✅ Обновляем UI для воды
            }
        }


        public string CalorieDisplayText => $"Ккал: {CurrentCalories:F1} / {CalorieNorm:F0}";


        public CalculateNutritionViewModel(UserInfoModel userInfo)
        {
            UserInfo = userInfo ?? new UserInfoModel();
            SavedProducts = new ObservableCollection<NutritionModel>();
            _repository = new CalculateNutritionRepository();

            CalorieNorm = CalculateDailyCalorieIntake(UserInfo);
            CalculateMacroNorms();

            Console.WriteLine("🚀 Инициализация диаграммы...");

            WaterChartSeries = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "Випито",
                        Values = new ChartValues<double> { 0 },
                        Fill = (Brush)new BrushConverter().ConvertFromString("#7ACEFF"), // Голубой
                        DataLabels = true
                    },
                    new PieSeries
                    {
                        Title = "Залишилось",
                        Values = new ChartValues<double> { Math.Max(1, WaterNorm) },
                        Fill = (Brush)new BrushConverter().ConvertFromString("#BEBEBE"), // Серый
                        DataLabels = true
                    }
                };

            if (CalorieChartSeries == null)
            {
                CalorieChartSeries = new SeriesCollection
{
                new PieSeries
                {
                    Title = "З'їли",
                    Values = new ChartValues<double> { 1 },
                    Fill = (Brush)new BrushConverter().ConvertFromString("#70A93D"), // Зеленый #70A93D
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "Залишилось",
                    Values = new ChartValues<double> { Math.Max(1, CalorieNorm) },
                    Fill = (Brush)new BrushConverter().ConvertFromString("#BEBEBE"), // Серый #BEBEBE
                    DataLabels = true
                }
            };

            }

            Console.WriteLine("✅ Диаграмма инициализирована!");
            UpdatePieChart();
        }

        private void UpdateWaterChart()
        {
            if (WaterChartSeries == null || WaterChartSeries.Count < 2)
                return;

            double consumed = Math.Max(0, CurrentWater);
            double remaining = Math.Max(0, WaterNorm - CurrentWater);

            if (consumed == 0 && remaining == 0) // Если воды нет, показываем пустую диаграмму
            {
                remaining = WaterNorm > 0 ? WaterNorm : 1;
            }

            WaterChartSeries[0].Values = new ChartValues<double> { consumed };
            WaterChartSeries[1].Values = new ChartValues<double> { remaining };

            OnPropertyChanged(nameof(WaterChartSeries));
        }

        public void UpdatePieChart()
        {
            if (CalorieChartSeries == null || CalorieChartSeries.Count < 2)
                return;

            double consumed = Math.Max(0, CurrentCalories);
            double remaining = Math.Max(0, CalorieNorm - CurrentCalories);

            if (consumed == 0 && remaining == 0) // Если продуктов нет, показываем пустую диаграмму
            {
                remaining = CalorieNorm > 0 ? CalorieNorm : 1;
            }

            CalorieChartSeries[0].Values = new ChartValues<double> { consumed };
            CalorieChartSeries[1].Values = new ChartValues<double> { remaining };

            OnPropertyChanged(nameof(CalorieChartSeries));
        }

        public void AddProduct(NutritionModel product)
        {
            if (product != null)
            {
                CurrentCalories += product.Calories;
                CurrentProtein += product.Protein;
                CurrentFats += product.Fats;
                CurrentCarbs += product.Carbs;

                if (product.Title.ToLower() == "вода" || product.Title.ToLower() == "water")
                {
                    CurrentWater += product.Weight;
                    OnPropertyChanged(nameof(WaterDisplayText)); // ✅ Обновляем UI
                    UpdateWaterChart(); // ✅ Обновляем диаграмму воды
                }

                UpdatePieChart(); // ✅ Обновляем диаграмму калорий
            }
        }

        public async Task<NutritionModel> SearchAndAddProductAsync(string query, double weight)
        {
            if (weight <= 0)
            {
                throw new ArgumentException("Вага повинна бути більше 0 г.");
            }

            string lowerQuery = query.Trim().ToLower();

            // ✅ Если пользователь вводит "вода", обрабатываем её отдельно
            if (lowerQuery == "вода" || lowerQuery == "water")
            {
                var waterProduct = new NutritionModel
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

                SavedProducts.Add(waterProduct);
                CurrentWater += weight;
                OnPropertyChanged(nameof(WaterDisplayText));
                UpdateWaterChart();

                Console.WriteLine($"✅ Добавлена вода: {weight} мл в {waterProduct.Time}");
                return waterProduct;
            }

            var products = await _repository.GetProductsAsync(query);

            if (products == null || products.Count == 0)
            {
                Console.WriteLine($"❌ Продукт '{query}' не найден. Открываю форму ввода.");

                // ✅ Сразу показываем форму БЕЗ MessageBox
                ShowManualEntryOverlayAction?.Invoke(query, weight);

                return null;
            }

            var product = products[0];
            product.Weight = weight;
            product.Calories = (product.Calories * weight) / 100;
            product.Protein = (product.Protein * weight) / 100;
            product.Fats = (product.Fats * weight) / 100;
            product.Carbs = (product.Carbs * weight) / 100;
            product.Time = DateTime.Now.ToString("HH:mm");

            SavedProducts.Add(product);
            CurrentCalories += product.Calories;
            CurrentProtein += product.Protein;
            CurrentFats += product.Fats;
            CurrentCarbs += product.Carbs;

            Console.WriteLine($"✅ Додано продукт: {product.Title}, калорії: {product.Calories}, Время: {product.Time}");

            UpdatePieChart();
            return product;
        }


        private void UpdateCalorieText()
        {
            OnPropertyChanged(nameof(CalorieDisplayText));
        }


        private NutritionModel ShowManualProductEntryDialog(string productName, double weight)
        {
            var dialog = new ManualProductEntryDialog(productName, weight);
            if (dialog.ShowDialog() == true)
            {
                var manualProduct = dialog.GetEnteredProduct();
                if (manualProduct != null)
                {
                    SavedProducts.Add(manualProduct);
                    CurrentCalories += manualProduct.Calories;
                    CurrentProtein += manualProduct.Protein;
                    CurrentFats += manualProduct.Fats;
                    CurrentCarbs += manualProduct.Carbs;

                    Console.WriteLine($"✅ Вручную добавлен продукт: {manualProduct.Title}, калорії: {manualProduct.Calories}");

                    UpdatePieChart();
                    return manualProduct;
                }
            }

            return null; // Если пользователь нажал "Скасувати"
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public double CalculateDailyCalorieIntake(UserInfoModel userInfo)

        {
            if (userInfo == null || userInfo.Weight <= 0 || userInfo.Height <= 0 || userInfo.Age <= 0)
            {
                return 0;
            }

            double bmr;
            if (userInfo.Gender == "Чоловік" || userInfo.Gender == "Мужчина")
            {
                bmr = 88.36 + (13.4 * userInfo.Weight) + (4.8 * userInfo.Height) - (5.7 * userInfo.Age);
            }
            else
            {
                bmr = 447.6 + (9.2 * userInfo.Weight) + (3.1 * userInfo.Height) - (4.3 * userInfo.Age);
            }

            double activityMultiplier = userInfo.ActivityLevel switch
            {
                "Низька" => 1.2,
                "Середня" => 1.375,
                "Висока" => 1.55,
                "Дуже висока" => 1.725,
                "Екстремальна" => 1.9,
                _ => 1.2
            };

            double tdee = bmr * activityMultiplier;

            tdee = userInfo.Purpose switch
            {
                "Схуднення" => tdee * 0.85,
                "Набір маси" => tdee * 1.15,
                _ => tdee
            };

            return Math.Round(tdee);
        }

    }
}
