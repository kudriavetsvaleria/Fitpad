using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class CalculateNutritionViewModel
    {
        private readonly CalculateNutritionRepository _repository;
        public ObservableCollection<NutritionModel> SavedProducts { get; set; }

        public CalculateNutritionViewModel()
        {
            _repository = new CalculateNutritionRepository();
            SavedProducts = new ObservableCollection<NutritionModel>();
        }

        public async Task<NutritionModel> SearchAndAddProductAsync(string query, double weight)
        {
            if (weight <= 0)
            {
                throw new ArgumentException("Вага повинна бути більше 0 г.");
            }

            var products = await _repository.GetProductsAsync(query);

            if (products.Count > 0)
            {
                var product = products[0];

                if (string.IsNullOrWhiteSpace(product.Title))
                {
                    product.Title = query;
                }

                // Пересчитываем КБЖУ под введенное пользователем количество грамм
                double factor = weight / 100.0;
                product.Calories *= factor;
                product.Protein *= factor;
                product.Fats *= factor;
                product.Carbs *= factor;
                product.Weight = weight;

                SavedProducts.Add(product);
                return product;
            }

            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
