using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using lab_1.Models;
using lab_1.Services;
using lab_1.Views;
using Serilog;

namespace lab_1.ViewModels
{
    public class StudentViewModel : IQueryAttributable, INotifyPropertyChanged
    {
        private readonly ILogger _logger = Log.ForContext<StudentViewModel>();
        private readonly ISessionService _session;
        private readonly IAuthService _authService;

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

        // Властивості для прив'язки в UI залежно від ролі (Лаб 6)
        public bool IsAdminPanelVisible => _session.IsInRole(UserRole.Administrator);
        public string CurrentUserInfo => $"Користувач: {_session.CurrentUser ?? "Гість"} ({_session.CurrentRole?.ToString() ?? "Без ролі"})";

        public ICommand AddStudentCommand { get; }
        public ICommand OpenDetailsCommand { get; }
        public ICommand DeleteStudentCommand { get; }
        public ICommand ManageUsersCommand { get; }
        public ICommand LogoutCommand { get; }

        public StudentViewModel(ISessionService session, IAuthService authService)
        {
            _session = session;
            _authService = authService;

            Students = new ObservableCollection<Student>
            {
                new Student { Id = 1, FullName = "Іваненко Іван", Group = "2-4", AverageScore = 4.5 },
                new Student { Id = 2, FullName = "Петренко Петро", Group = "2-4", AverageScore = 3.8 }
            };

            _logger.Information("StudentViewModel initialized with {InitialCount} students for role {Role}",
                Students.Count, _session.CurrentRole);

            AddStudentCommand = new Command(AddStudent);
            OpenDetailsCommand = new Command<Student>(async student => await OpenDetailsAsync(student));
            DeleteStudentCommand = new Command<Student>(async student => await DeleteStudentAsync(student));
            ManageUsersCommand = new Command(async () => await ManageUsersAsync());
            LogoutCommand = new Command(async () => await LogoutAsync());
        }

        public void RefreshRoleState()
        {
            OnPropertyChanged(nameof(IsAdminPanelVisible));
            OnPropertyChanged(nameof(CurrentUserInfo));
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

        // Перевірка ролі на рівні команди для критичної дії (Лаб 6, п. 4)
        private async Task DeleteStudentAsync(Student? student)
        {
            if (student == null)
                return;

            if (!_session.IsInRole(UserRole.Administrator))
            {
                _logger.Warning("Access denied: user {User} ({Role}) tried to delete student Id={Id}",
                    _session.CurrentUser, _session.CurrentRole, student.Id);

                await Shell.Current.DisplayAlert(
                    "Доступ заборонено",
                    "Видалення студентів доступне лише користувачам із роллю Administrator.",
                    "ОК");
                return;
            }

            Students.Remove(student);
            _logger.Information("Administrator {User} deleted student Id={Id} ({FullName})",
                _session.CurrentUser, student.Id, student.FullName);
        }

        // Дія, видима лише адміністратору в UI (Лаб 6, п. 3)
        private async Task ManageUsersAsync()
        {
            if (!_session.IsInRole(UserRole.Administrator))
            {
                await Shell.Current.DisplayAlert("Доступ заборонено", "Дія доступна лише адміністратору.", "ОК");
                return;
            }

            await Shell.Current.DisplayAlert(
                "Керування користувачами",
                "Панель адміністратора: активні ролі Dispatcher та Administrator.",
                "ОК");
        }

        private async Task LogoutAsync()
        {
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
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