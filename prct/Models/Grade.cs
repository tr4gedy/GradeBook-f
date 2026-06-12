using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeBook.Models
{
    public class Grade
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }

        [Range(2, 5, ErrorMessage = "Оценка должна быть в диапазоне 2..5")]
        public int Value { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow.Date;

        [ForeignKey(nameof(StudentId))]
        public virtual Student? Student { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual Subject? Subject { get; set; }
    }
}