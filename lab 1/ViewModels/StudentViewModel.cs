using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using lab_1.Models;

namespace lab_1.ViewModels
{
    public class StudentViewModel : IQueryAttributable, INotifyPropertyChanged
    {
        public ObservableCollection<Student> Students { get; set; }

        // Властивості для форми додавання (з Лаби 1)
        private string _fullName;
        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }

        private string _group;
        public string Group { get => _group; set { _group = value; OnPropertyChanged(); } }

        private double _averageScore;
        public double AverageScore { get => _averageScore; set { _averageScore = value; OnPropertyChanged(); } }

        // Команди
        public ICommand AddStudentCommand { get; }
        public ICommand OpenDetailsCommand { get; }

        public StudentViewModel()
        {
            // Початковий список студентів
            Students = new ObservableCollection<Student>
            {
                new Student { Id = 1, FullName = "Іваненко Іван", Group = "2-4", AverageScore = 4.5 },
                new Student { Id = 2, FullName = "Петренко Петро", Group = "2-4", AverageScore = 3.8 }
            };

            // Команда додавання студента 
            AddStudentCommand = new Command(() =>
            {
                if (!string.IsNullOrWhiteSpace(FullName))
                {
                    int newId = Students.Count > 0 ? Students.Max(s => s.Id) + 1 : 1;
                    Students.Add(new Student { Id = newId, FullName = FullName, Group = Group, AverageScore = AverageScore });

                    // Очищаємо поля після додавання
                    FullName = string.Empty;
                    Group = string.Empty;
                    AverageScore = 0;
                }
            });

            // Команда відкриття сторінки деталей (Лаба 2)
            OpenDetailsCommand = new Command<Student>(async (student) =>
            {
                if (student == null) return;
                var parameters = new Dictionary<string, object> { { "SelectedStudent", student } };
                await Shell.Current.GoToAsync("studentdetail", parameters);
            });
        }

        // Отримання даних після редагування на сторінці деталей
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("UpdatedStudent", out var updatedObj) && updatedObj is Student updatedStudent)
            {
                var existingStudent = Students.FirstOrDefault(s => s.Id == updatedStudent.Id);
                if (existingStudent != null)
                {
                    existingStudent.FullName = updatedStudent.FullName;
                    existingStudent.Group = updatedStudent.Group;
                    existingStudent.AverageScore = updatedStudent.AverageScore;

                    // Перемальовуємо список
                    var index = Students.IndexOf(existingStudent);
                    Students[index] = existingStudent;
                }
                query.Clear();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}