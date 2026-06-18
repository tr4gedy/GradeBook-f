using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GradeBook.Data;
using GradeBook.Models;

namespace GradeBook.Services
{
    public class GradeService : IGradeService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public GradeService(IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Group>> GetGroupsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Groups.OrderBy(g => g.Name).ToListAsync();
        }

        public async Task<List<Subject>> GetSubjectsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Subjects.OrderBy(s => s.Title).ToListAsync();
        }

        public async Task<List<Student>> GetStudentsAsync(int groupId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Students
                .AsNoTracking() // <--- Добавьте это
                .Where(s => s.GroupId == groupId)
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }

        public async Task<double> CalculateAverageAsync(int studentId, int? subjectId = null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.Grades.Where(g => g.StudentId == studentId);

            if (subjectId.HasValue)
            {
                query = query.Where(g => g.SubjectId == subjectId.Value);
            }

            if (!await query.AnyAsync()) return 0;
            return await query.AverageAsync(g => g.Value);
        }

        public async Task<double> CalculateGroupAverageAsync(int groupId, int? subjectId = null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.Grades.Where(g => g.Student.GroupId == groupId);

            if (subjectId.HasValue)
            {
                query = query.Where(g => g.SubjectId == subjectId.Value);
            }

            if (!await query.AnyAsync()) return 0;
            return await query.AverageAsync(g => g.Value);
        }

        public async Task<Group> AddGroupAsync(string name)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var group = new Group { Name = name };
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            return group;
        }

        public async Task<bool> DeleteGroupAsync(int groupId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var group = await db.Groups.FindAsync(groupId);
            if (group == null) return false;

            db.Groups.Remove(group);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<Student> AddStudentAsync(int groupId, string fullName)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var student = new Student { GroupId = groupId, FullName = fullName };
            db.Students.Add(student);
            await db.SaveChangesAsync();
            return student;
        }

        public async Task<bool> DeleteStudentAsync(int studentId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var student = await db.Students.FindAsync(studentId);
            if (student == null) return false;

            db.Students.Remove(student);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task AddGradeAsync(int studentId, int subjectId, int value)
        {
            if (value < 2 || value > 5)
                throw new ArgumentOutOfRangeException(nameof(value), "Оценка должна быть в диапазоне от 2 до 5.");

            await using var db = await _dbFactory.CreateDbContextAsync();
            var grade = new Grade
            {
                StudentId = studentId,
                SubjectId = subjectId,
                Value = value
            };
            db.Grades.Add(grade);
            await db.SaveChangesAsync();
        }

        public async Task UpdateRatingAsync(int groupId)
        {
            // Метод для обновления снэпшотов/рейтинга (при необходимости добавьте сюда логику)
            await Task.CompletedTask;
        }

        public async Task<Grade?> GetLastGradeAsync(int studentId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Grades
                .Where(g => g.StudentId == studentId)
                .Include(g => g.Subject) // Важно! Загружаем связанный предмет
                .OrderByDescending(g => g.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Grade>> GetStudentGradesAsync(int studentId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Grades
                .Where(g => g.StudentId == studentId)
                .Include(g => g.Subject)
                .ToListAsync();
        }

        public async Task UpdateGradeAsync(int gradeId, int newValue)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var grade = await db.Grades.FindAsync(gradeId);
            if (grade != null)
            {
                grade.Value = newValue;
                await db.SaveChangesAsync();
            }
        }

        public async Task<bool> DeleteGradeAsync(int gradeId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var grade = await db.Grades.FindAsync(gradeId);
            if (grade == null) return false;

            db.Grades.Remove(grade);
            await db.SaveChangesAsync();
            return true;
        }
    }
}