using System.Collections.Generic;
using System.Threading.Tasks;
using GradeBook.Models;

namespace GradeBook.Services
{
    public interface IGradeService
    {
        Task<List<Group>> GetGroupsAsync();
        Task<List<Subject>> GetSubjectsAsync();
        Task<List<Student>> GetStudentsAsync(int groupId);

        // Исправлено: добавлены необязательные параметры для совместимости
        Task<double> CalculateAverageAsync(int studentId, int? subjectId = null);
        Task<double> CalculateGroupAverageAsync(int groupId, int? subjectId = null);

        Task<Group> AddGroupAsync(string name);
        Task<bool> DeleteGroupAsync(int groupId);
        Task<Student> AddStudentAsync(int groupId, string fullName);
        Task<bool> DeleteStudentAsync(int studentId);
        Task AddGradeAsync(int studentId, int subjectId, int value);
        Task UpdateRatingAsync(int groupId);

        Task<Grade?> GetLastGradeAsync(int studentId);

        // Методы управления оценками, которые запрашивает интерфейс
        Task<List<Grade>> GetStudentGradesAsync(int studentId);
        Task UpdateGradeAsync(int gradeId, int newValue);
        Task<bool> DeleteGradeAsync(int gradeId);
    }
}