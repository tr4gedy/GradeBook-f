using System.Collections.Generic;
using System.Threading.Tasks;
using GradeBook.Models;

namespace GradeBook.Services
{
    public interface IGradeService
    {
        // Группы
        Task<List<Group>> GetGroupsAsync();
        Task<Group> AddGroupAsync(string name);
        Task<bool> DeleteGroupAsync(int groupId);

        // Студенты
        Task<List<Student>> GetStudentsAsync(int groupId);
        Task<Student> AddStudentAsync(int groupId, string fullName);
        Task<bool> DeleteStudentAsync(int studentId);

        // Дисциплины
        Task<List<Subject>> GetSubjectsAsync();

        // Оценки
        Task AddGradeAsync(int studentId, int subjectId, int value);
        Task<double> CalculateAverageAsync(int studentId);
        Task<double> CalculateGroupAverageAsync(int groupId);
        Task UpdateRatingAsync(int groupId);
        Task<(string SubjectTitle, int Value)?> GetLastGradeAsync(int studentId);
    }
}