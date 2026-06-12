using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using GradeBook.Models;
using GradeBook.Services;

namespace GradeBook.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IGradeService _gradeService;
        private readonly IPdfExporter _pdfExporter;

        #region Collections

        public ObservableCollection<Group> Groups { get; } = new();
        public ObservableCollection<Student> Students { get; } = new();
        public ObservableCollection<Subject> Subjects { get; } = new();

        #endregion

        #region Selected Properties

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StudentAverage))]
        [NotifyCanExecuteChangedFor(nameof(AddGradeCommand))]
        [NotifyCanExecuteChangedFor(nameof(AddStudentCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteStudentCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteGroupCommand))]
        private Group? _selectedGroup;

        partial void OnSelectedGroupChanged(Group? value)
        {
            SelectedStudent = null;
            if (value != null)
            {
                _ = SafeLoadStudentsAsync();
                _ = SafeUpdateGroupAverageAsync();
                _ = SafeRefreshRatingChartAsync();
            }
            else
            {
                Students.Clear();
                GroupAverage = 0;
                RatingSeries = Array.Empty<ISeries>();
                XAxes = Array.Empty<Axis>();
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StudentAverage))]
        [NotifyCanExecuteChangedFor(nameof(AddGradeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteStudentCommand))]
        private Student? _selectedStudent;

        partial void OnSelectedStudentChanged(Student? value)
        {
            _ = UpdateStudentAverageAsync();
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddGradeCommand))]
        private Subject? _selectedSubject;

        #endregion

        #region Properties (Исправленные приватные поля для генератора)

        [ObservableProperty]
        private double _studentAverage;

        [ObservableProperty]
        private double _groupAverage;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddGroupCommand))]
        private string _newGroupName = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddStudentCommand))]
        private string _newStudentFullName = string.Empty;

        [ObservableProperty]
        private ISeries[] _ratingSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] _xAxes = Array.Empty<Axis>();

        #endregion

        #region Constructor

        public MainViewModel(IGradeService gradeService, IPdfExporter pdfExporter)
        {
            _gradeService = gradeService;
            _pdfExporter = pdfExporter;
            _ = InitializeAsync();
        }

        #endregion

        #region Initialization

        private async Task InitializeAsync()
        {
            try
            {
                var groups = await _gradeService.GetGroupsAsync();
                foreach (var g in groups) Groups.Add(g);

                var subjects = await _gradeService.GetSubjectsAsync();
                foreach (var s in subjects) Subjects.Add(s);

                if (Groups.Any()) SelectedGroup = Groups.First();
                if (Subjects.Any()) SelectedSubject = Subjects.First();

                // #region agent log
                DebugLog.Write("H3,H4", "MainViewModel.cs:InitializeAsync", "initial data loaded", new { groupCount = Groups.Count, subjectCount = Subjects.Count, selectedGroupId = SelectedGroup?.Id, selectedSubjectId = SelectedSubject?.Id });
                // #endregion
            }
            catch (Exception ex)
            {
                // #region agent log
                DebugLog.Write("H3,H4", "MainViewModel.cs:InitializeAsync", "initialization exception", new { type = ex.GetType().Name, ex.Message, innerType = ex.InnerException?.GetType().Name, innerMessage = ex.InnerException?.Message });
                // #endregion

                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadStudentsAsync()
        {
            if (SelectedGroup == null) return;
            Students.Clear();
            var students = await _gradeService.GetStudentsAsync(SelectedGroup.Id);
            foreach (var s in students)
            {
                s.Average = await _gradeService.CalculateAverageAsync(s.Id);
                Students.Add(s);
            }

            // #region agent log
            DebugLog.Write("H4", "MainViewModel.cs:LoadStudentsAsync", "students loaded for selected group", new { selectedGroupId = SelectedGroup.Id, studentCount = Students.Count });
            // #endregion
        }

        private async Task UpdateGroupAverageAsync()
        {
            if (SelectedGroup == null) return;
            GroupAverage = await _gradeService.CalculateGroupAverageAsync(SelectedGroup.Id);
        }

        private async Task UpdateStudentAverageAsync()
        {
            if (SelectedStudent == null)
            {
                StudentAverage = 0;
                return;
            }
            try
            {
                StudentAverage = await _gradeService.CalculateAverageAsync(SelectedStudent.Id);
                SelectedStudent.Average = StudentAverage;
            }
            catch
            {
                StudentAverage = 0;
            }
        }

        #endregion

        #region Commands - Groups

        [RelayCommand(CanExecute = nameof(CanAddGroup))]
        private async Task AddGroupAsync()
        {
            var groupName = NewGroupName.Trim();

            try
            {
                var newGroup = await _gradeService.AddGroupAsync(groupName);
                Groups.Add(newGroup);
                SelectedGroup = newGroup;
                NewGroupName = string.Empty;
                LogCommandSuccess("AddGroup", new { newGroupId = newGroup.Id, totalGroups = Groups.Count });

                MessageBox.Show($"Группа '{newGroup.Name}' добавлена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogCommandException("AddGroup", ex);
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanAddGroup() => !string.IsNullOrWhiteSpace(NewGroupName);

        [RelayCommand(CanExecute = nameof(CanDeleteGroup))]
        private async Task DeleteGroupAsync()
        {
            if (SelectedGroup == null) return;
            var groupToDelete = SelectedGroup;
            var result = MessageBox.Show(
                $"Удалить группу '{groupToDelete.Name}'? Все студенты и оценки будут удалены!",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var deleted = await _gradeService.DeleteGroupAsync(groupToDelete.Id);
                if (!deleted)
                {
                    MessageBox.Show("Группа уже удалена или не найдена.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Groups.Remove(groupToDelete);
                SelectedGroup = Groups.FirstOrDefault();
                LogCommandSuccess("DeleteGroup", new { deletedGroupId = groupToDelete.Id, totalGroups = Groups.Count, selectedGroupId = SelectedGroup?.Id });
                if (SelectedGroup == null)
                {
                    Students.Clear();
                    SelectedStudent = null;
                    GroupAverage = 0;
                    RatingSeries = Array.Empty<ISeries>();
                    XAxes = Array.Empty<Axis>();
                }
            }
            catch (Exception ex)
            {
                LogCommandException("DeleteGroup", ex);
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanDeleteGroup() => SelectedGroup != null;

        #endregion

        #region Commands - Students

        [RelayCommand(CanExecute = nameof(CanAddStudent))]
        private async Task AddStudentAsync()
        {
            if (SelectedGroup == null) return;
            var fullName = NewStudentFullName.Trim();

            try
            {
                var newStudent = await _gradeService.AddStudentAsync(SelectedGroup.Id, fullName);
                newStudent.Average = 0;
                Students.Add(newStudent);
                SelectedStudent = newStudent;
                NewStudentFullName = string.Empty;
                LogCommandSuccess("AddStudent", new { newStudentId = newStudent.Id, selectedGroupId = SelectedGroup.Id, totalStudents = Students.Count });

                await UpdateGroupAverageAsync();
                await RefreshRatingChartAsync();

                MessageBox.Show($"Студент '{newStudent.FullName}' добавлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogCommandException("AddStudent", ex);
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanAddStudent() =>
            SelectedGroup != null && !string.IsNullOrWhiteSpace(NewStudentFullName);

        [RelayCommand(CanExecute = nameof(CanDeleteStudent))]
        private async Task DeleteStudentAsync()
        {
            if (SelectedStudent == null) return;
            var studentToDelete = SelectedStudent;
            var result = MessageBox.Show(
                $"Удалить студента '{studentToDelete.FullName}'? Все его оценки будут удалены.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var deleted = await _gradeService.DeleteStudentAsync(studentToDelete.Id);
                if (!deleted)
                {
                    MessageBox.Show("Студент уже удалён или не найден.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Students.Remove(studentToDelete);
                SelectedStudent = null;
                LogCommandSuccess("DeleteStudent", new { deletedStudentId = studentToDelete.Id, selectedGroupId = SelectedGroup?.Id, totalStudents = Students.Count });
                await UpdateGroupAverageAsync();
                await RefreshRatingChartAsync();
            }
            catch (Exception ex)
            {
                LogCommandException("DeleteStudent", ex);
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanDeleteStudent() => SelectedStudent != null;

        #endregion

        #region Commands - Grades

        [RelayCommand(CanExecute = nameof(CanAddGrade))]
        private async Task AddGradeAsync()
        {
            if (SelectedStudent == null || SelectedSubject == null) return;
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Оценка (2..5) по '{SelectedSubject.Title}' для {SelectedStudent.FullName}:",
                "Новая оценка", "5");
            if (!int.TryParse(input, out int value))
            {
                MessageBox.Show("Введите число.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var student = SelectedStudent;
                await _gradeService.AddGradeAsync(student.Id, SelectedSubject.Id, value);
                await UpdateStudentAverageAsync();
                student.Average = await _gradeService.CalculateAverageAsync(student.Id);
                StudentAverage = student.Average;
                await UpdateGroupAverageAsync();
                await RefreshRatingChartAsync();
                MessageBox.Show("Оценка добавлена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                var details = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Ошибка: {details}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanAddGrade() => SelectedStudent != null && SelectedSubject != null;

        #endregion

        #region Commands - Export & Rating

        [RelayCommand]
        private async Task ExportPdfAsync()
        {
            if (SelectedGroup == null || !Students.Any())
            {
                MessageBox.Show("Нет данных для экспорта.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var fileName = $"Vedomost_{SelectedGroup.Name}_{DateTime.Today:yyyy-MM-dd}.pdf";
                var reportRows = new List<StudentReportRow>();
                foreach (var student in Students)
                {
                    var avg = await _gradeService.CalculateAverageAsync(student.Id);
                    var lastGrade = await _gradeService.GetLastGradeAsync(student.Id);
                    reportRows.Add(new StudentReportRow
                    {
                        FullName = student.FullName,
                        Subject = lastGrade?.SubjectTitle ?? "Нет оценок",
                        Grade = lastGrade?.Value ?? 0,
                        Average = avg
                    });
                }

                await _pdfExporter.ExportAsync(fileName, SelectedGroup.Name, reportRows);
                MessageBox.Show($"Сохранено:\n{System.IO.Path.GetFullPath(fileName)}",
                    "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var details = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Ошибка: {details}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RefreshRatingAsync()
        {
            if (SelectedGroup == null) return;
            try
            {
                await _gradeService.UpdateRatingAsync(SelectedGroup.Id);
                await RefreshRatingChartAsync();
                MessageBox.Show("Рейтинг обновлен.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var details = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Ошибка: {details}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Chart Logic

        private async Task RefreshRatingChartAsync()
        {
            if (SelectedGroup == null) return;
            var students = await _gradeService.GetStudentsAsync(SelectedGroup.Id);
            var ratedStudents = new List<(string Name, double Avg)>();
            foreach (var s in students)
            {
                var avg = await _gradeService.CalculateAverageAsync(s.Id);
                ratedStudents.Add((s.FullName, avg));
            }

            var sorted = ratedStudents
                .OrderByDescending(x => x.Avg)
                .ThenBy(x => x.Name)
                .ToList();

            RatingSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = sorted.Select(x => x.Avg).ToList(),
                    Name = "Средний балл",
                    Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(
                        new SkiaSharp.SKColor(65, 105, 225))
                }
            };

            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = sorted.Select(x => x.Name).ToList(),
                    LabelsRotation = 45,
                    Name = "Студенты"
                }
            };
        }

        #endregion

        #region Safe Async Wrappers

        private async Task SafeLoadStudentsAsync()
        {
            try { await LoadStudentsAsync(); }
            catch (Exception ex) { MessageBox.Show($"Ошибка загрузки: {ex.Message}"); }
        }

        private async Task SafeUpdateGroupAverageAsync()
        {
            try { await UpdateGroupAverageAsync(); }
            catch (Exception ex) { MessageBox.Show($"Ошибка расчёта: {ex.Message}"); }
        }

        private async Task SafeRefreshRatingChartAsync()
        {
            try { await RefreshRatingChartAsync(); }
            catch (Exception ex) { MessageBox.Show($"Ошибка графика: {ex.Message}"); }
        }

        private void LogCommandSuccess(string commandName, object data)
        {
            // #region agent log
            DebugLog.Write("H5", "MainViewModel.cs:LogCommandSuccess", "command completed", new { commandName, data });
            // #endregion
        }

        private void LogCommandException(string commandName, Exception ex)
        {
            // #region agent log
            DebugLog.Write("H5", "MainViewModel.cs:LogCommandException", "command exception", new { commandName, type = ex.GetType().Name, ex.Message, innerType = ex.InnerException?.GetType().Name, innerMessage = ex.InnerException?.Message, selectedGroupId = SelectedGroup?.Id, selectedStudentId = SelectedStudent?.Id, selectedSubjectId = SelectedSubject?.Id });
            // #endregion
        }

        #endregion
    }
}