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
        private double _currentCalories;
        public double CurrentCalories
        {
            get => _currentCalories;
            set
            {
                _currentCalories = value;
                OnPropertyChanged();
            }
        }

        private double _currentProteins;
        public double CurrentProteins
        {
            get => _currentProteins;
            set
            {
                _currentProteins = value;
                OnPropertyChanged();
            }
        }

        private double _currentFats;
        public double CurrentFats
        {
            get => _currentFats;
            set
            {
                _currentFats = value;
                OnPropertyChanged();
            }
        }

        private double _currentCarbs;
        public double CurrentCarbs
        {
            get => _currentCarbs;
            set
            {
                _currentCarbs = value;
                OnPropertyChanged();
            }
        }


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
