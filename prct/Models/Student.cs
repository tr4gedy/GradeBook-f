// Models/Student.cs
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace GradeBook.Models
{
    public class Student : INotifyPropertyChanged
    {
        private double _average;

        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public virtual Group? Group { get; set; }
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

        [NotMapped]
        public double Average
        {
            get => _average;
            set
            {
                if (System.Math.Abs(_average - value) < 0.001) return;
                _average = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}