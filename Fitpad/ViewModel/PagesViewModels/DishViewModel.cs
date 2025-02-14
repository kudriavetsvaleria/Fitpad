using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using Google.Cloud.Firestore;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class DishViewModel : INotifyPropertyChanged
    {
        private readonly DishRepository _repository;
        private ObservableCollection<DishModel> _dishes;

        public ObservableCollection<DishModel> Dishes
        {
            get => _dishes;
            set
            {
                _dishes = value;
                OnPropertyChanged();
            }
        }

        public DishViewModel(FirestoreDb firestoreDb)
        {
            _repository = new DishRepository(firestoreDb);
            Dishes = new ObservableCollection<DishModel>();
        }

        public async Task LoadUserDishesAsync()
        {
            if (string.IsNullOrEmpty(UserSession.CurrentUserId))
            {
                Console.WriteLine("❌ Ошибка: UserId отсутствует!");
                return;
            }

            var dishes = await _repository.GetUserDishesAsync(UserSession.CurrentUserId);
            Dishes.Clear();
            foreach (var dish in dishes)
            {
                Dishes.Add(dish);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
