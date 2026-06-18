using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeBook.Services
{
    public interface IPdfExporter
    {
        Task ExportAsync(string path, string groupName, IEnumerable<StudentReportRow> rows);
    }

    public class StudentReportRow
    {
        public string FullName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string AllGrades { get; set; } = string.Empty; // Было Grade (int), стало AllGrades (string)
        public double Average { get; set; }
    }
}