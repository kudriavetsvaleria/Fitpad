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
        private readonly Google.Cloud.Firestore.FirestoreDb _db;
        private readonly DishRepository _repository;
        private ObservableCollection<DishModel> _dishes;

        public ObservableCollection<DishModel> Dishes { get; }


        public DishViewModel(FirestoreDb db)
        {
            _db = db;
            Dishes = new ObservableCollection<DishModel>();
        }

        public async Task LoadUserDishesAsync(string userId)
        {
            Dishes.Clear();

            var snapshot = await _db
                .Collection("Users").Document(userId)
                .Collection("Dishes")
                .GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                if (doc.Exists)
                    Dishes.Add(doc.ConvertTo<DishModel>());
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
