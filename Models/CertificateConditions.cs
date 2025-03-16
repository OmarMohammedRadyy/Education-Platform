using System.ComponentModel.DataAnnotations;

namespace EducationPlatformN.Models
{
    public class CertificateConditionViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }
        public List<CertificateCondition> Conditions { get; set; }
    }
    public class CertificateCondition
    {
        [Key]
        public int ConditionId { get; set; }

        [Required]
        public string ConditionType { get; set; } // مثل "PassPercentage", "MaxAttempts"

        [Required]
        public decimal Value { get; set; } // القيمة (مثل 70 لنسبة النجاح)
        [Required(ErrorMessage = "يرجى اختيار دورة صالحة")]
        public int CourseId { get; set; } // جعله nullable        public Course Course { get; set; }
        public string Description { get; set; }
        public Course Course { get; set; }
    }
}