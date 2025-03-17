using System.ComponentModel.DataAnnotations;

namespace EducationPlatformN.Models
{
    public class Course
    {
        public int CourseId { get; set; }

        [Required]
        public string Title { get; set; } = "بدون عنوان"; // قيمة افتراضية

        [Required]
        public string Description { get; set; } = "بدون وصف"; // قيمة افتراضية

        [Required]
        public decimal Price { get; set; } = 0.00m; // قيمة افتراضية

        public int? TeacherId { get; set; } // يمكن أن يكون null

        [Required]
        public string ThumbnailUrl { get; set; } = "/images/default-thumbnail.jpg"; // قيمة افتراضية

        public string IntroVideoUrl { get; set; } = ""; // قيمة افتراضية

        public DateTime CreatedAt { get; set; } = DateTime.Now; // قيمة افتراضية

        public string InvoiceUrl { get; set; } = ""; // قيمة افتراضية

        public virtual User Teacher { get; set; } // العلاقة مع Teacher
        public List<Lesson> Lessons { get; set; } = new List<Lesson>();
        public List<StudentProgress> StudentProgress { get; set; } = new List<StudentProgress>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public virtual ICollection<CertificateCondition> CertificateConditions { get; set; } = new List<CertificateCondition>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<FinalExam> FinalExams { get; set; } = new List<FinalExam>();
    }
}