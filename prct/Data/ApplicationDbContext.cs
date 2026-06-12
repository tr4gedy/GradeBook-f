using GradeBook.Models;
using Microsoft.EntityFrameworkCore;

namespace GradeBook.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Grade> Grades => Set<Grade>();
        public DbSet<RatingSnapshot> RatingSnapshots => Set<RatingSnapshot>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ===== Группа =====
            modelBuilder.Entity<Group>(entity =>
            {
                entity.ToTable("groups");
                entity.Property(g => g.Id).HasColumnName("id");
                entity.Property(g => g.Name).HasColumnName("name");
                entity.HasIndex(g => g.Name).IsUnique();
                entity.Property(g => g.Name).HasMaxLength(50).IsRequired();
            });

            // ===== Студент =====
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("students");
                entity.Property(s => s.Id).HasColumnName("id");
                entity.Property(s => s.FullName).HasColumnName("fullname");
                entity.Property(s => s.GroupId).HasColumnName("groupid");
                entity.Property(s => s.FullName).HasMaxLength(100).IsRequired();

                entity.HasOne(s => s.Group)
                      .WithMany(g => g.Students)
                      .HasForeignKey(s => s.GroupId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Дисциплина =====
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.ToTable("subjects");
                entity.Property(s => s.Id).HasColumnName("id");
                entity.Property(s => s.Title).HasColumnName("title");
                entity.HasIndex(s => s.Title).IsUnique();
                entity.Property(s => s.Title).HasMaxLength(100).IsRequired();
            });

            // ===== Оценка =====
            modelBuilder.Entity<Grade>(entity =>
            {
                entity.ToTable("grades");
                entity.Property(g => g.Id).HasColumnName("id");
                entity.Property(g => g.StudentId).HasColumnName("studentid");
                entity.Property(g => g.SubjectId).HasColumnName("subjectid");
                entity.Property(g => g.Value).HasColumnName("value");
                entity.Property(g => g.Date).HasColumnName("date").HasColumnType("date");

                entity.HasOne(g => g.Student)
                      .WithMany(s => s.Grades)
                      .HasForeignKey(g => g.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(g => g.Subject)
                      .WithMany(s => s.Grades)
                      .HasForeignKey(g => g.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasCheckConstraint("CK_Grades_Value", "\"value\" BETWEEN 2 AND 5");
            });

            // ===== Снимок рейтинга =====
            modelBuilder.Entity<RatingSnapshot>(entity =>
            {
                entity.ToTable("ratingsnapshots");
                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.StudentId).HasColumnName("studentid");
                entity.Property(r => r.AverageGrade).HasColumnName("averagegrade");
                entity.Property(r => r.SnapshotDate).HasColumnName("snapshotdate").HasColumnType("date");

                entity.HasOne<Student>()
                      .WithMany()
                      .HasForeignKey(r => r.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(r => r.AverageGrade).HasPrecision(3, 2);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}