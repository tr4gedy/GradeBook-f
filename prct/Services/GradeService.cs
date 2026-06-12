using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GradeBook.Data;
using GradeBook.Models;
using Microsoft.EntityFrameworkCore;

namespace GradeBook.Services
{
    public class GradeService : IGradeService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private int _activeOperations;
        private long _nextOperationId;

        public GradeService(IDbContextFactory<ApplicationDbContext> dbFactory) => _dbFactory = dbFactory;

        // ===== ГРУППЫ =====

        public async Task<List<Group>> GetGroupsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Groups.OrderBy(g => g.Name).ToListAsync();
        }

        /// <summary>
        /// Добавление новой группы с проверкой уникальности имени.
        /// </summary>
        public async Task<Group> AddGroupAsync(string name)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var normalizedName = name?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
                throw new ArgumentException("Название группы не может быть пустым.", nameof(name));

            if (normalizedName.Length > 50)
                throw new ArgumentException("Название группы не может превышать 50 символов.", nameof(name));

            var normalizedNameLower = normalizedName.ToLowerInvariant();
            var exists = await db.Groups.AnyAsync(g => g.Name.ToLower() == normalizedNameLower);
            if (exists)
                throw new InvalidOperationException($"Группа '{normalizedName}' уже существует.");

            var group = new Group { Name = normalizedName };
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            return group;
        }

        /// <summary>
        /// Удаление группы с каскадным удалением студентов и оценок.
        /// </summary>
        public async Task<bool> DeleteGroupAsync(int groupId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var group = await db.Groups
                .Include(g => g.Students)
                .ThenInclude(s => s.Grades)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group is null) return false;

            db.Groups.Remove(group);
            await db.SaveChangesAsync();
            return true;
        }

        // ===== СТУДЕНТЫ =====

        public async Task<List<Student>> GetStudentsAsync(int groupId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var operationId = LogOperationStart("GetStudentsAsync", "H6,H9", db.ContextId.InstanceId, new { groupId });
            try
            {
                return await db.Students
                    .Where(s => s.GroupId == groupId)
                    .OrderBy(s => s.FullName)
                    .ToListAsync();
            }
            finally
            {
                LogOperationEnd("GetStudentsAsync", "H6,H9", operationId, db.ContextId.InstanceId);
            }
        }

        /// <summary>
        /// Добавление студента в группу с проверкой ФИО.
        /// </summary>
        public async Task<Student> AddStudentAsync(int groupId, string fullName)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var operationId = LogOperationStart("AddStudentAsync", "H7,H8,H9", db.ContextId.InstanceId, new { groupId, fullNameLength = fullName?.Length });
            var normalizedFullName = fullName?.Trim();
            try
            {
                if (string.IsNullOrWhiteSpace(normalizedFullName))
                    throw new ArgumentException("ФИО студента не может быть пустым.", nameof(fullName));

                if (normalizedFullName.Length > 100)
                    throw new ArgumentException("ФИО студента не может превышать 100 символов.", nameof(fullName));

                var groupExists = await db.Groups.AnyAsync(g => g.Id == groupId);
                if (!groupExists)
                    throw new InvalidOperationException("Группа не найдена.");

                var normalizedFullNameLower = normalizedFullName.ToLowerInvariant();
                var exists = await db.Students.AnyAsync(s =>
                    s.GroupId == groupId && s.FullName.ToLower() == normalizedFullNameLower);
                if (exists)
                    throw new InvalidOperationException($"Студент '{normalizedFullName}' уже есть в выбранной группе.");

                var student = new Student
                {
                    FullName = normalizedFullName,
                    GroupId = groupId
                };

                db.Students.Add(student);
                await db.SaveChangesAsync();
                return student;
            }
            catch (Exception ex)
            {
                LogException("AddStudentAsync", "H7,H8,H9", db.ContextId.InstanceId, ex, new { groupId });
                throw;
            }
            finally
            {
                LogOperationEnd("AddStudentAsync", "H7,H8,H9", operationId, db.ContextId.InstanceId);
            }
        }

        /// <summary>
        /// Удаление студента с каскадным удалением оценок.
        /// </summary>
        public async Task<bool> DeleteStudentAsync(int studentId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var student = await db.Students.FindAsync(studentId);
            if (student is null) return false;

            db.Students.Remove(student);
            await db.SaveChangesAsync();
            return true;
        }

        // ===== ДИСЦИПЛИНЫ =====

        public async Task<List<Subject>> GetSubjectsAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Subjects.OrderBy(s => s.Title).ToListAsync();
        }

        // ===== ОЦЕНКИ =====

        public async Task AddGradeAsync(int studentId, int subjectId, int value)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var operationId = LogOperationStart("AddGradeAsync", "H7,H8,H9", db.ContextId.InstanceId, new { studentId, subjectId, value });
            try
            {
                if (value < 2 || value > 5)
                    throw new ArgumentOutOfRangeException(nameof(value), "Оценка должна быть 2, 3, 4 или 5.");

                db.Grades.Add(new Grade
                {
                    StudentId = studentId,
                    SubjectId = subjectId,
                    Value = value,
                    Date = DateTime.UtcNow.Date
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogException("AddGradeAsync", "H7,H8,H9", db.ContextId.InstanceId, ex, new { studentId, subjectId, value });
                throw;
            }
            finally
            {
                LogOperationEnd("AddGradeAsync", "H7,H8,H9", operationId, db.ContextId.InstanceId);
            }
        }

        public async Task<double> CalculateAverageAsync(int studentId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var operationId = LogOperationStart("CalculateAverageAsync", "H6,H7,H9", db.ContextId.InstanceId, new { studentId });
            try
            {
                var grades = await db.Grades.Where(g => g.StudentId == studentId).ToListAsync();
                return grades.Count == 0 ? 0 : grades.Average(g => g.Value);
            }
            finally
            {
                LogOperationEnd("CalculateAverageAsync", "H6,H7,H9", operationId, db.ContextId.InstanceId);
            }
        }

        public async Task<double> CalculateGroupAverageAsync(int groupId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var operationId = LogOperationStart("CalculateGroupAverageAsync", "H6,H7,H9", db.ContextId.InstanceId, new { groupId });
            try
            {
                var grades = await db.Grades
                    .Where(g => g.Student.GroupId == groupId)
                    .ToListAsync();
                return grades.Count == 0 ? 0 : grades.Average(g => g.Value);
            }
            finally
            {
                LogOperationEnd("CalculateGroupAverageAsync", "H6,H7,H9", operationId, db.ContextId.InstanceId);
            }
        }

        public async Task UpdateRatingAsync(int groupId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var students = await db.Students.Where(s => s.GroupId == groupId).ToListAsync();
            var today = DateTime.UtcNow.Date;

            foreach (var s in students)
            {
                var avg = await CalculateAverageAsync(s.Id);
                db.RatingSnapshots.Add(new RatingSnapshot
                {
                    StudentId = s.Id,
                    AverageGrade = (decimal)Math.Round(avg, 2),
                    SnapshotDate = today
                });
            }
            await db.SaveChangesAsync();
        }

        public async Task<(string SubjectTitle, int Value)?> GetLastGradeAsync(int studentId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var grade = await db.Grades
                .Where(g => g.StudentId == studentId)
                .OrderByDescending(g => g.Date)
                .Include(g => g.Subject)
                .FirstOrDefaultAsync();

            return grade == null ? null : (grade.Subject!.Title, grade.Value);
        }

        private long LogOperationStart(string operationName, string hypothesisId, Guid dbContextId, object data)
        {
            var operationId = Interlocked.Increment(ref _nextOperationId);
            var activeOperations = Interlocked.Increment(ref _activeOperations);

            // #region agent log
            DebugLog.Write(hypothesisId, "GradeService.cs:LogOperationStart", "service operation started", new { operationName, operationId, activeOperations, dbContextId, data });
            // #endregion

            return operationId;
        }

        private void LogOperationEnd(string operationName, string hypothesisId, long operationId, Guid dbContextId)
        {
            var activeOperations = Interlocked.Decrement(ref _activeOperations);

            // #region agent log
            DebugLog.Write(hypothesisId, "GradeService.cs:LogOperationEnd", "service operation ended", new { operationName, operationId, activeOperations, dbContextId });
            // #endregion
        }

        private void LogException(string operationName, string hypothesisId, Guid dbContextId, Exception ex, object data)
        {
            // #region agent log
            DebugLog.Write(hypothesisId, "GradeService.cs:LogException", "service operation exception", new { operationName, type = ex.GetType().Name, ex.Message, innerType = ex.InnerException?.GetType().Name, innerMessage = ex.InnerException?.Message, dbContextId, data });
            // #endregion
        }
    }
}