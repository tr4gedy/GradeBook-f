// Models/Subject.cs
using System.Collections.Generic;

namespace GradeBook.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}