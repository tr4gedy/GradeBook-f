
namespace GradeBook.Models
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
}