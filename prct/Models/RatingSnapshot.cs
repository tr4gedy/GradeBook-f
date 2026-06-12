// Models/RatingSnapshot.cs
using System;

namespace GradeBook.Models
{
    public class RatingSnapshot
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public decimal AverageGrade { get; set; }
        public DateTime SnapshotDate { get; set; } = DateTime.UtcNow.Date;
        public virtual Student? Student { get; set; }
    }
}
