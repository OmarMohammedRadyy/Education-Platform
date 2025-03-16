using System.ComponentModel.DataAnnotations;

namespace EducationPlatformN.Models
{
    public class AddTrueFalseQuestionViewModel
    {
        [Required(ErrorMessage = "نص السؤال مطلوب")]
        [StringLength(500, ErrorMessage = "نص السؤال لا يمكن أن يتجاوز 500 حرف")]
        public string QuestionText { get; set; }

        [Required(ErrorMessage = "الإجابة الصحيحة مطلوبة")]
        public string CorrectAnswer { get; set; } // "True" أو "False"
    }
}