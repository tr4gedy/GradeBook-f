using GradeBook.Data;
using GradeBook.Models;
using GradeBook.Services;
using Microsoft.EntityFrameworkCore;

using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace GradeBook.Tests
{
    public class GradeBookIntegrationTests : IDisposable
    {
        private readonly DbContextOptions<ApplicationDbContext> _contextOptions;

        public GradeBookIntegrationTests()
        {
            // Используем уникальное имя БД для интеграционного сценария
            _contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql($"Host=localhost;Database=IntegrationTests_{Guid.NewGuid()};Username=postgres;Password=sa")
                .Options;

            using var context = new ApplicationDbContext(_contextOptions);
            context.Database.EnsureCreated();
        }

        [Fact]
        public async Task IssueBook_Integration_WithActiveFine_ShouldFailAndLogException()
        {
            await using (var context = new ApplicationDbContext(_contextOptions))
            {
                var group = new GradeBook.Models.Group { Id = 10, Name = "Тест-Группа" };
                var student = new Student { Id = 10, FullName = "Должник И.И.", GroupId = 10 };

                context.Groups.Add(group);
                context.Students.Add(student);

                var subject = new Subject { Id = 1, Title = "Архитектура СУБД" };
                context.Subjects.Add(subject);
                context.Grades.Add(new Grade { StudentId = 10, SubjectId = 1, Value = 2, Date = DateTime.UtcNow.AddDays(-1) });

                await context.SaveChangesAsync();
            }

            // Act (Выполнение операции через сервис)
            var dbFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
            dbFactoryMock.Setup(f => f.CreateDbContextAsync(default))
                         .ReturnsAsync(() => new ApplicationDbContext(_contextOptions));

            var service = new GradeService(dbFactoryMock.Object);

            await service.UpdateRatingAsync(10);

            // Assert (Проверка результатов во всей цепочке таблиц)
            await using (var verifyContext = new ApplicationDbContext(_contextOptions))
            {
                var snapshot = await verifyContext.RatingSnapshots
                    .FirstOrDefaultAsync(r => r.StudentId == 10);

                Assert.NotNull(snapshot);
                Assert.Equal(2.00m, snapshot.AverageGrade);
            }
        }

        public void Dispose()
        {
            using var context = new ApplicationDbContext(_contextOptions);
            context.Database.EnsureDeleted();
        }
    }
}