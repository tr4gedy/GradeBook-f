using System;
using System.Collections.Generic;
using System.Windows;
using GradeBook.Models;
using GradeBook.Services;

namespace GradeBook.Views
{
    public partial class GradesWindow : Window
    {
        private readonly Student _student;
        private readonly IGradeService _gradeService;

        public GradesWindow(Student student, IGradeService gradeService)
        {
            InitializeComponent();
            _student = student;
            _gradeService = gradeService;
            TitleText.Text = $"История оценок: {_student.FullName}";
            LoadGrades();
        }

        private async void LoadGrades()
        {
            try
            {
                GradesGrid.ItemsSource = await _gradeService.GetStudentGradesAsync(_student.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки оценок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var grades = GradesGrid.ItemsSource as List<Grade>;
            if (grades == null) return;

            try
            {
                foreach (var grade in grades)
                {
                    if (grade.Value < 2 || grade.Value > 5)
                    {
                        MessageBox.Show("Оценка должна быть числом от 2 до 5!", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    await _gradeService.UpdateGradeAsync(grade.Id, grade.Value);
                }
                MessageBox.Show("Изменения успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (GradesGrid.SelectedItem is Grade selectedGrade)
            {
                var confirm = MessageBox.Show("Вы хотите полностью удалить выбранную оценку?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    await _gradeService.DeleteGradeAsync(selectedGrade.Id);
                    LoadGrades();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите оценку из списка для удаления.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}