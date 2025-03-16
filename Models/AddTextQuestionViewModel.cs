using System.ComponentModel.DataAnnotations;

namespace EducationPlatformN.Models
{
    public class AddTextQuestionViewModel
    {
        [Required(ErrorMessage = "نص السؤال مطلوب")]
        [StringLength(500, ErrorMessage = "نص السؤال لا يمكن أن يتجاوز 500 حرف")]
        public string QuestionText { get; set; }

        [Required(ErrorMessage = "الإجابة الصحيحة مطلوبة")]
        [StringLength(1000, ErrorMessage = "الإجابة الصحيحة لا يمكن أن تتجاوز 1000 حرف")]
        public string CorrectAnswer { get; set; }
    }
}