using System.ComponentModel.DataAnnotations;

namespace EducationPlatformN.Models
{
    public class FinalExamQuestion
    {
        public int FinalExamQuestionId { get; set; }
        [Required(ErrorMessage = "معرف الاختبار النهائي مطلوب")]
        public int FinalExamId { get; set; }
        [Required(ErrorMessage = "نص السؤال مطلوب")]
        public string QuestionText { get; set; }
        [Required(ErrorMessage = "الإجابة الصحيحة مطلوبة")]
        public string CorrectAnswer { get; set; }
        [Required(ErrorMessage = "نوع السؤال مطلوب")]
        public string QuestionType { get; set; } // "MultipleChoice", "TrueFalse", "Text"
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public virtual FinalExam FinalExam { get; set; }
    }
}
