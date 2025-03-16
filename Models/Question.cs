using System.ComponentModel.DataAnnotations;

namespace EducationPlatformN.Models
{
    public class Question
    {
        public int QuestionId { get; set; }
        public int LessonId { get; set; }
        [Required(ErrorMessage = "نص السؤال مطلوب")]
        public string QuestionText { get; set; }
        [Required(ErrorMessage = "نوع السؤال مطلوب")]
        public string QuestionType { get; set; }
        public string? OptionA { get; set; } // اختياري
        public string? OptionB { get; set; } // اختياري
        public string? OptionC { get; set; } // اختياري
        public string? OptionD { get; set; } // اختياري
        [Required(ErrorMessage = "الإجابة الصحيحة مطلوبة")]
        public string CorrectAnswer { get; set; }
        public List<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>(); // تصحيح إلى قائمة
        public virtual Lesson Lesson { get; set; }

    }
}
        
   

