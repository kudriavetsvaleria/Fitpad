using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class CalculateNutritionViewModel
    {
        private readonly CalculateNutritionRepository _repository;
        public ObservableCollection<SavedProductModel> SavedProducts { get; set; }

        public CalculateNutritionViewModel()
        {
            _repository = new CalculateNutritionRepository();
            SavedProducts = new ObservableCollection<SavedProductModel>();
        }

        public async Task<SavedProductModel> SearchAndAddProductAsync(string query)
        {
            var products = await _repository.GetProductsAsync(query);

            if (products.Count > 0)
            {
                var product = products[0]; // Берем первый найденный продукт
                SavedProducts.Add(product);
                return product;
            }

            return null;
        }
    }
}
