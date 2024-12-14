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
        private ObservableCollection<NutritionModel> _nutritionCards;
        public ObservableCollection<NutritionModel> NutritionCards
        {
            get => _nutritionCards;
            set
            {
                _nutritionCards = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNutritionEmpty)); // Обновляем состояние пустоты
            }
        }
        public bool IsNutritionEmpty => NutritionCards == null || NutritionCards.Count == 0; // Если список пуст
        private readonly NutritionRepository _repository;

        public NutritionViewModel()
        {
            _repository = new NutritionRepository();
            NutritionCards = new ObservableCollection<NutritionModel>();
        }



        public async Task LoadNutritionAsync(bool useRandom, int offset)
        {
            NutritionCards.Clear(); // Полная загрузка очищает текущий список
            var recipes = await _repository.GetRecipesAsync(useRandom, offset);
            foreach (var recipe in recipes)
            {
                NutritionCards.Add(recipe);
            }
        }

        public async Task LoadMoreNutritionAsync(int offset)
        {
            var recipes = await _repository.GetRecipesAsync(false, offset);
            foreach (var recipe in recipes)
            {
                NutritionCards.Add(recipe);
            }
        }

        private bool _isSearchEmpty;
        public bool IsSearchEmpty
        {
            get => _isSearchEmpty;
            set
            {
                _isSearchEmpty = value;
                OnPropertyChanged(); // Уведомляем привязку об изменении
            }
        }

        public async Task SearchNutritionAsync(string query)
        {
            NutritionCards.Clear(); // Очистка текущего списка

            var recipes = await _repository.SearchRecipesAsync(query);
            foreach (var recipe in recipes)
            {
                NutritionCards.Add(recipe);
            }

            // Устанавливаем IsSearchEmpty только после завершения поиска
            IsSearchEmpty = NutritionCards.Count == 0;

            // Принудительное обновление свойства IsNutritionEmpty
            OnPropertyChanged(nameof(IsNutritionEmpty));
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
