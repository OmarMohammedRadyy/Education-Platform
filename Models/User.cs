using System.ComponentModel.DataAnnotations;

namespace EducationPlatformN.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = "بدون اسم"; // قيمة افتراضية

        [Required]
        [EmailAddress]
        public string Email { get; set; } = ""; // قيمة افتراضية

        [Required]
        [Phone]
        public string Phone { get; set; } = ""; // قيمة افتراضية

        [Required]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; } = ""; // قيمة افتراضية

        [Required]
        public string Role { get; set; } = "Unknown"; // قيمة افتراضية

        public DateTime CreatedAt { get; set; } = DateTime.Now; // قيمة افتراضية

        [Required]
        public string Language { get; set; } = "ar"; // قيمة افتراضية

        public virtual ICollection<Course> Courses { get; set; } = new List<Course>(); // كمعلم
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
        public virtual ICollection<QuizResult> QuizResults { get; set; } = new List<QuizResult>();
        public virtual ICollection<StudentProgress> StudentProgress { get; set; } = new List<StudentProgress>();
        public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<FinalExamResult> FinalExamResults { get; set; } = new List<FinalExamResult>();
    }
}