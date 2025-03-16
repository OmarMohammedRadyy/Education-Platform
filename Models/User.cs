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

        public List<Course> Courses { get; set; } = new List<Course>(); // للمدرسين
        public List<Payment> Payments { get; set; } = new List<Payment>();
        public List<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public List<QuizResult> QuizResults { get; set; } = new List<QuizResult>();
        public List<StudentProgress> StudentProgress { get; set; } = new List<StudentProgress>();
        public List<Certificate> Certificates { get; set; } = new List<Certificate>();
    }
}