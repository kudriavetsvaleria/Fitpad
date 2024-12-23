using Fitpad.Model.Entities;
using Fitpad.Model.Repositories;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Fitpad.ViewModel.PagesViewModels
{
    public class CaloriesViewModel : INotifyPropertyChanged
    {
        private readonly CaloriesRepository _repository;
        private CaloriesModel _caloriesData;

        public CaloriesModel CaloriesData
        {
            get => _caloriesData;
            set
            {
                _caloriesData = value;
                OnPropertyChanged();
            }
        }

        public CaloriesViewModel()
        {
            _repository = new CaloriesRepository();
            CaloriesData = new CaloriesModel();
        }

        public void Calculate(double weight, double height, int age, string gender, double activityLevel)
        {
            CaloriesData = _repository.CalculateCalories(weight, height, age, gender, activityLevel);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
