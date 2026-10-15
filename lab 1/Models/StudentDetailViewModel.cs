using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using lab_1.Models;

namespace lab_1.ViewModels
{
    public class StudentDetailViewModel : IQueryAttributable, INotifyPropertyChanged
    {
        private Student _currentStudent;

        public string FullName { get; set; }
        public string Group { get; set; }

        private double _averageScore;
        public double AverageScore
        {
            get => _averageScore;
            set { if (_averageScore != value) { _averageScore = value; OnPropertyChanged(); } }
        }

        public ICommand GoBackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public StudentDetailViewModel()
        {
            GoBackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

            SaveCommand = new Command(async () =>
            {
                if (_currentStudent != null)
                {
                    _currentStudent.FullName = FullName;
                    _currentStudent.Group = Group;
                    _currentStudent.AverageScore = AverageScore;

                    var parameters = new Dictionary<string, object> { { "UpdatedStudent", _currentStudent } };
                    await Shell.Current.GoToAsync("..", parameters);
                }
            });

            ToggleThemeCommand = new Command(() =>
            {
                if (Application.Current.UserAppTheme == AppTheme.Dark)
                {
                    Application.Current.UserAppTheme = AppTheme.Light;
                    Preferences.Set("AppTheme", "Light");
                }
                else
                {
                    Application.Current.UserAppTheme = AppTheme.Dark;
                    Preferences.Set("AppTheme", "Dark");
                }
            });
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("SelectedStudent", out var value) && value is Student student)
            {
                _currentStudent = student;
                FullName = student.FullName;
                Group = student.Group;
                AverageScore = student.AverageScore;

                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(Group));
                OnPropertyChanged(nameof(AverageScore));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}