using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using lab_1.Models;
using Serilog;

namespace lab_1.ViewModels
{
    public class StudentViewModel : IQueryAttributable, INotifyPropertyChanged
    {
        private readonly ILogger _logger = Log.ForContext<StudentViewModel>();

        public ObservableCollection<Student> Students { get; set; }

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set
            {
                _fullName = value;
                OnPropertyChanged();
            }
        }

        private string _group = string.Empty;
        public string Group
        {
            get => _group;
            set
            {
                _group = value;
                OnPropertyChanged();
            }
        }

        private double _averageScore;
        public double AverageScore
        {
            get => _averageScore;
            set
            {
                _averageScore = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddStudentCommand { get; }
        public ICommand OpenDetailsCommand { get; }

        public StudentViewModel()
        {
            Students = new ObservableCollection<Student>
            {
                new Student { Id = 1, FullName = "Іваненко Іван", Group = "2-4", AverageScore = 4.5 },
                new Student { Id = 2, FullName = "Петренко Петро", Group = "2-4", AverageScore = 3.8 }
            };

            _logger.Information("StudentViewModel initialized with {InitialCount} students", Students.Count);

            AddStudentCommand = new Command(AddStudent);
            OpenDetailsCommand = new Command<Student>(async student => await OpenDetailsAsync(student));
        }

        private void AddStudent()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FullName))
                {
                    _logger.Warning("Attempted to add student with empty FullName");
                    return;
                }

                int newId = Students.Count > 0 ? Students.Max(s => s.Id) + 1 : 1;
                var student = new Student
                {
                    Id = newId,
                    FullName = FullName.Trim(),
                    Group = Group?.Trim() ?? string.Empty,
                    AverageScore = AverageScore
                };

                Students.Add(student);
                _logger.Information("Added student Id={Id}, Name={FullName}, Group={Group}, Avg={Score}",
                    student.Id, student.FullName, student.Group, student.AverageScore);

                FullName = string.Empty;
                Group = string.Empty;
                AverageScore = 0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to add student {FullName}", FullName);
            }
        }

        private async Task OpenDetailsAsync(Student? student)
        {
            if (student == null)
            {
                _logger.Warning("OpenDetailsCommand invoked with null student");
                return;
            }

            try
            {
                _logger.Information("Navigating to details for student Id={Id} ({FullName})", student.Id, student.FullName);
                var parameters = new Dictionary<string, object> { { "SelectedStudent", student } };
                await Shell.Current.GoToAsync("studentdetail", parameters);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Navigation to studentdetail failed for Id={Id}", student.Id);
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            try
            {
                if (query.TryGetValue("UpdatedStudent", out var updatedObj) && updatedObj is Student updatedStudent)
                {
                    var existingStudent = Students.FirstOrDefault(s => s.Id == updatedStudent.Id);
                    if (existingStudent != null)
                    {
                        existingStudent.FullName = updatedStudent.FullName;
                        existingStudent.Group = updatedStudent.Group;
                        existingStudent.AverageScore = updatedStudent.AverageScore;

                        var index = Students.IndexOf(existingStudent);
                        Students[index] = existingStudent;

                        _logger.Information("Updated student Id={Id} -> {FullName}, Group={Group}, Avg={Score}",
                            existingStudent.Id, existingStudent.FullName, existingStudent.Group, existingStudent.AverageScore);
                    }
                    else
                    {
                        _logger.Warning("UpdatedStudent with Id={Id} not found in list", updatedStudent.Id);
                    }

                    query.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed applying query attributes");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}