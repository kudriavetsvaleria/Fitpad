using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Fitpad.View.Components;
using System.Windows;
using Fitpad.View;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class CalculateNutritionViewModel : INotifyPropertyChanged
    {
        private readonly CalculateNutritionRepository _repository;

        private double _currentCalories;
        private double _calorieNorm;

        private double _currentProtein, _currentFats, _currentCarbs, _currentWater;
        private double _proteinNorm, _fatsNorm, _carbsNorm, _waterNorm;

        public UserInfoModel UserInfo { get; private set; }
        public ObservableCollection<NutritionModel> SavedProducts { get; set; }

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
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalorieDisplayText)); // Обновляем отображение
            }
        }

        public double CalorieNorm
        {
            get => _calorieNorm;
            set
            {
                _calorieNorm = value;
                OnPropertyChanged(nameof(CalorieDisplayText)); // ✅ UI теперь обновляется при изменении
            }
        }

        public string CalorieDisplayText => $"Ккал: {CurrentCalories:F1} / {CalorieNorm:F0}";



        public CalculateNutritionViewModel(UserInfoModel userInfo)
        {
            UserInfo = userInfo ?? new UserInfoModel();
            _repository = new CalculateNutritionRepository();
            SavedProducts = new ObservableCollection<NutritionModel>();
            CalculateDailyNutritionNorms();
            // ✅ Вызываем метод пересчета нормы калорий
            CalorieNorm = CalculateDailyCalorieIntake(UserInfo);
        }

        private void CalculateDailyNutritionNorms()
        {
            double calorieNorm = CalculateDailyCalorieIntake(UserInfo);

            // ✅ Белки: 15% от калорийности (1 г = 4 ккал)
            ProteinNorm = Math.Round((calorieNorm * 0.15) / 4);

            // ✅ Жиры: 25% от калорийности (1 г = 9 ккал)
            FatsNorm = Math.Round((calorieNorm * 0.25) / 9);

            // ✅ Углеводы: 55% от калорийности (1 г = 4 ккал)
            CarbsNorm = Math.Round((calorieNorm * 0.55) / 4);

            // ✅ Вода: 35 мл на 1 кг веса
            WaterNorm = Math.Round(UserInfo.Weight * 35);

            // ✅ Обновляем UI
            OnPropertyChanged(nameof(ProteinDisplayText));
            OnPropertyChanged(nameof(FatsDisplayText));
            OnPropertyChanged(nameof(CarbsDisplayText));
            OnPropertyChanged(nameof(WaterDisplayText));
        }


        public void AddProduct(NutritionModel product)
        {
            if (product != null)
            {
                CurrentProtein += product.Protein;
                CurrentFats += product.Fats;
                CurrentCarbs += product.Carbs;
                CurrentWater += product.Water; // ✅ Добавляем воду

                // ✅ Обновляем UI
                OnPropertyChanged(nameof(ProteinDisplayText));
                OnPropertyChanged(nameof(FatsDisplayText));
                OnPropertyChanged(nameof(CarbsDisplayText));
                OnPropertyChanged(nameof(WaterDisplayText));
            }
        }


        public async Task<NutritionModel> SearchAndAddProductAsync(string query, double weight)
        {
            if (weight <= 0)
            {
                throw new ArgumentException("Вага повинна бути більше 0 г.");
            }

            string lowerQuery = query.Trim().ToLower();

            // ✅ Если введена вода, создаем продукт вручную с текущим временем
            if (lowerQuery == "вода" || lowerQuery == "water")
            {
                var waterProduct = new NutritionModel
                {
                    Title = "Вода",
                    Weight = weight,
                    Calories = 0,  // У воды 0 калорий
                    Protein = 0,
                    Fats = 0,
                    Carbs = 0,
                    Water = weight, // ✅ Количество воды равно введенному весу
                    Time = DateTime.Now.ToString("HH:mm") // ✅ Записываем текущее время в формате ЧЧ:ММ
                };

                // ✅ Добавляем в таблицу и в поле воды
                SavedProducts.Add(waterProduct);
                CurrentWater += weight;

                OnPropertyChanged(nameof(WaterDisplayText));
                Console.WriteLine($"✅ Добавлена вода: {weight} мл в {waterProduct.Time}");

                return waterProduct;
            }

            // ✅ Запрашиваем продукт из API/БД
            var products = await _repository.GetProductsAsync(query);

            if (products.Count > 0)
            {
                var product = products[0];

                if (string.IsNullOrWhiteSpace(product.Title))
                {
                    product.Title = query;
                }

                double factor = weight / 100.0;
                product.Calories *= factor;
                product.Protein *= factor;
                product.Fats *= factor;
                product.Carbs *= factor;
                product.Weight = weight;
                product.Time = DateTime.Now.ToString("HH:mm"); // ✅ Добавляем текущее время

                SavedProducts.Add(product);
                CurrentCalories += product.Calories;
                CurrentProtein += product.Protein;
                CurrentFats += product.Fats;
                CurrentCarbs += product.Carbs;

                Console.WriteLine($"✅ Додано продукт: {product.Title}, калорії: {product.Calories}, Вода: {product.Water}, Время: {product.Time}");

                return product;
            }

            // ❌ Если продукт не найден — вызываем окно ручного ввода
            return ShowManualProductEntryDialog(query, weight);
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
