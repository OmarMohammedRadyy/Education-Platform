using System.ComponentModel.DataAnnotations;

namespace EducationPlatformN.Models
{
    public class FinalExam
    {
        public int FinalExamId { get; set; }
        
        public int CourseId { get; set; }
        [Required(ErrorMessage = "العنوان مطلوب")]
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now; // قيمة افتراضية
        public virtual Course Course { get; set; }
        public virtual ICollection<FinalExamResult> FinalExamResults { get; set; } = new List<FinalExamResult>();
        public virtual ICollection<FinalExamQuestion> FinalExamQuestions { get; set; } = new List<FinalExamQuestion>();
    }
}
