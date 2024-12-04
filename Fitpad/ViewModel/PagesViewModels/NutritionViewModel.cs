using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class NutritionViewModel : INotifyPropertyChanged
    {
        private readonly NutritionRepository _repository;

        private ObservableCollection<NutritionModel> _nutritionCards;
        public ObservableCollection<NutritionModel> NutritionCards
        {
            get => _nutritionCards;
            set
            {
                _nutritionCards = value;
                OnPropertyChanged();
            }
        }

        public NutritionViewModel()
        {
            _repository = new NutritionRepository();
            NutritionCards = new ObservableCollection<NutritionModel>();
        }

        public async Task LoadNutritionAsync(bool useRandom = false, int offset = 0)
        {
            if (NutritionCards.Count > 0) return;

            var recipes = await _repository.GetRecipesAsync(useRandom, offset);
            foreach (var recipe in recipes)
            {
                NutritionCards.Add(recipe);
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
