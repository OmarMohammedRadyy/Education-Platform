using System.ComponentModel.DataAnnotations;

namespace EducationPlatformN.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now; // قيمة افتراضية

        public string PaymentProofUrl { get; set; } = ""; // قيمة افتراضية

        [Required]
        public string Status { get; set; } = "Pending"; // قيمة افتراضية

        public string RejectionReason { get; set; } = ""; // قيمة افتراضية

        public string InvoiceUrl { get; set; } = ""; // قيمة افتراضية

        public bool IsActive { get; set; } = true; // قيمة افتراضية

        public DateTime? LastAccessed { get; set; } // يمكن أن يكون null

        public virtual User User { get; set; } // العلاقة مع User
        public virtual Course Course { get; set; } // العلاقة مع Course
    }
}