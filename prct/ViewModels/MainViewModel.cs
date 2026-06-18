using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GradeBook.Models;
using GradeBook.Services;
using GradeBook.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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
        [NotifyCanExecuteChangedFor(nameof(ExportPdfCommand))]
        private Group? _selectedGroup;

        partial void OnSelectedGroupChanged(Group? value)
        {
            SelectedStudent = null;

            if (value != null)
            {
                _ = ReloadGroupAsync();
            }
        }

        private async Task ReloadGroupAsync()
        {
            await LoadStudentsAsync();
            await UpdateGroupAverageAsync();
            await RefreshRatingChartAsync();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StudentAverage))]
        [NotifyCanExecuteChangedFor(nameof(AddGradeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteStudentCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenGradesWindowCommand))]
        private Student? _selectedStudent;

        partial void OnSelectedStudentChanged(Student? value)
        {
            _ = UpdateStudentAverageAsync();
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddGradeCommand))]
        private Subject? _selectedSubject;

        partial void OnSelectedSubjectChanged(Subject? value)
        {
            _ = ReloadForSelectedSubjectAsync();
        }

        private async Task ReloadForSelectedSubjectAsync()
        {
            if (SelectedGroup == null)
                return;

            // Запоминаем выбранного студента
            int? selectedStudentId = SelectedStudent?.Id;

            // Загружаем студентов заново
            Students.Clear();

            var students = (await _gradeService.GetStudentsAsync(SelectedGroup.Id))
    .GroupBy(s => s.Id)
    .Select(g => g.First())
    .ToList();

            foreach (var student in students)
            {
                student.Average = await _gradeService.CalculateAverageAsync(
                    student.Id,
                    SelectedSubject?.Id);

                Students.Add(student);
            }

            // Возвращаем выделение
            if (selectedStudentId.HasValue)
            {
                SelectedStudent = Students.FirstOrDefault(s => s.Id == selectedStudentId.Value);
            }

            // Пересчитываем средний балл выбранного студента
            if (SelectedStudent != null)
            {
                StudentAverage = SelectedStudent.Average;
            }
            else
            {
                StudentAverage = 0;
            }

            // Пересчитываем средний по группе
            GroupAverage = await _gradeService.CalculateGroupAverageAsync(
                SelectedGroup.Id,
                SelectedSubject?.Id);

            // Обновляем гистограмму
            RatingSeries = new ISeries[]
            {
        new ColumnSeries<double>
        {
            Name = "Средний балл",
            Values = Students.Select(s => s.Average).ToArray()
        }
            };

            XAxes = new[]
            {
        new Axis
        {
            Labels = Students.Select(s => s.FullName).ToArray(),
            LabelsRotation = 15
        }
    };

            OnPropertyChanged(nameof(Students));
            OnPropertyChanged(nameof(RatingSeries));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(GroupAverage));
            OnPropertyChanged(nameof(StudentAverage));
        }
        #endregion

        #region Properties
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

        #region Initialization & Data Loading
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadStudentsAsync()
        {
            if (SelectedGroup == null) return;

            SelectedStudent = null;
      

            Students.Clear();

            var students = (await _gradeService.GetStudentsAsync(SelectedGroup.Id))
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .ToList();

            foreach (var student in students)
            {
                student.Average = await _gradeService.CalculateAverageAsync(
                    student.Id,
                    SelectedSubject?.Id);

                if (!Students.Any(existing => existing.Id == student.Id))
                {
                    Students.Add(student);
                }
            }
        }

        private async Task UpdateGroupAverageAsync()
        {
            if (SelectedGroup == null) return;
            GroupAverage = await _gradeService.CalculateGroupAverageAsync(SelectedGroup.Id, SelectedSubject?.Id);
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
                // ✅ ИСПРАВЛЕНО: Передаем 'SelectedStudent' вместо 'SelectedStudent.Id'
                StudentAverage = await _gradeService.CalculateAverageAsync(SelectedStudent.Id, SelectedSubject?.Id);
                SelectedStudent.Average = StudentAverage;
            }
            catch
            {
                StudentAverage = 0;
            }
        }

        private async Task RefreshRatingChartAsync()
        {
            if (SelectedGroup == null || Students.Count == 0)
            {
                RatingSeries = Array.Empty<ISeries>();
                XAxes = Array.Empty<Axis>();
                return;
            }

            // Берём только уникальных студентов
            var uniqueStudents = Students
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .OrderBy(s => s.FullName)
                .ToList();

            // Пересчитываем средние баллы
            foreach (var student in uniqueStudents)
            {
                student.Average = await _gradeService.CalculateAverageAsync(
                    student.Id,
                    SelectedSubject?.Id);
            }

            // Формируем гистограмму
            RatingSeries = new ISeries[]
            {
        new ColumnSeries<double>
        {
            Name = "Средний балл",
            Values = uniqueStudents
                .Select(s => s.Average)
                .ToArray()
        }
            };

            // Подписи по оси X
            XAxes = new Axis[]
            {
        new Axis
        {
            Labels = uniqueStudents
                .Select(s => s.FullName)
                .ToArray(),
            LabelsRotation = 15
        }
            };

            OnPropertyChanged(nameof(RatingSeries));
            OnPropertyChanged(nameof(XAxes));
        }
        #endregion

        #region Safe Wrappers
        private async Task SafeLoadStudentsAsync()
        {
            try { await LoadStudentsAsync(); }
            catch (Exception ex) { MessageBox.Show($"Ошибка загрузки студентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async Task SafeUpdateGroupAverageAsync()
        {
            try { await UpdateGroupAverageAsync(); }
            catch (Exception ex) { MessageBox.Show($"Ошибка расчета среднего балла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async Task SafeRefreshRatingChartAsync()
        {
            try { await RefreshRatingChartAsync(); }
            catch (Exception ex) { MessageBox.Show($"Ошибка обновления графика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
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
                MessageBox.Show($"Группа '{newGroup.Name}' добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
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
                if (!deleted) return;

                Groups.Remove(groupToDelete);
                SelectedGroup = Groups.FirstOrDefault();

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
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var newStudent = await _gradeService.AddStudentAsync(
                    SelectedGroup.Id,
                    fullName);

                await LoadStudentsAsync();

                SelectedStudent = Students.FirstOrDefault(s => s.Id == newStudent.Id);

                NewStudentFullName = string.Empty;

                await UpdateGroupAverageAsync();
                await RefreshRatingChartAsync();
                MessageBox.Show($"Студент '{newStudent.FullName}' добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool CanAddStudent() => SelectedGroup != null && !string.IsNullOrWhiteSpace(NewStudentFullName);

        [RelayCommand(CanExecute = nameof(CanDeleteStudent))]
        private async Task DeleteStudentAsync()
        {
            if (SelectedStudent == null) return;
            var studentToDelete = SelectedStudent;

            var result = MessageBox.Show($"Удалить студента '{studentToDelete.FullName}'? Все оценки будут удалены.", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var deleted = await _gradeService.DeleteStudentAsync(studentToDelete.Id);
                if (!deleted) return;

                Students.Remove(studentToDelete);
                SelectedStudent = null;

                await UpdateGroupAverageAsync();
                await RefreshRatingChartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

            if (string.IsNullOrWhiteSpace(input)) return;

            if (!int.TryParse(input, out int value))
            {
                MessageBox.Show("Введите корректное число.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var student = SelectedStudent;
                await _gradeService.AddGradeAsync(student.Id, SelectedSubject.Id, value);

                await LoadStudentsAsync();

                SelectedStudent =
                    Students.FirstOrDefault(x => x.Id == student.Id);

                await UpdateStudentAverageAsync();
                await UpdateGroupAverageAsync();
                await RefreshRatingChartAsync();

                MessageBox.Show(
                    "Оценка успешно добавлена!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool CanAddGrade() => SelectedStudent != null && SelectedSubject != null;

        [RelayCommand(CanExecute = nameof(CanOpenGradesWindow))]
        private async Task OpenGradesWindow()
        {
            if (SelectedStudent == null)
                return;

            var window = new GradesWindow(
                SelectedStudent,
                _gradeService)
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            window.ShowDialog();

            await LoadStudentsAsync();
            await UpdateStudentAverageAsync();
            await UpdateGroupAverageAsync();
            await RefreshRatingChartAsync();
        }
        private bool CanOpenGradesWindow() => SelectedStudent != null;
        #endregion

        #region Commands - Export PDF
        [RelayCommand]
        private async Task ExportPdfAsync()
        {
            if (SelectedGroup == null || !Students.Any()) return;

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Ведомость_{SelectedGroup.Name}.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var rows = new List<StudentReportRow>();
                    var subjectsDict = Subjects.ToDictionary(s => s.Id, s => s.Title);

                    foreach (var student in Students)
                    {
                        var allGradesForStudent = await _gradeService.GetStudentGradesAsync(student.Id);
                        var avg = await _gradeService.CalculateAverageAsync(student.Id, SelectedSubject?.Id);

                        // Группируем оценки по предметам для этого студента
                        var groupedGrades = allGradesForStudent.GroupBy(g => g.SubjectId);

                        foreach (var group in groupedGrades)
                        {
                            var subjectTitle = Subjects.FirstOrDefault(s => s.Id == group.Key)?.Title ?? "Неизвестно";
                            var gradesList = string.Join(", ", group.Select(g => g.Value.ToString()));

                            rows.Add(new StudentReportRow
                            {
                                FullName = student.FullName,
                                Subject = subjectTitle,
                                AllGrades = gradesList,
                                Average = avg
                            });
                        }
                    }

                    await _pdfExporter.ExportAsync(saveFileDialog.FileName, SelectedGroup.Name, rows);
                    MessageBox.Show("Ведомость успешно экспортирована в PDF!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте PDF: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private bool CanExportPdf() => SelectedGroup != null && Students.Any();
        #endregion
    }
}