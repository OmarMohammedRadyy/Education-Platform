using static EducationPlatformN.Controllers.QuizController;

namespace EducationPlatformN.Models
{
    public class QuizResultViewModel
    {
        public string LessonTitle { get; set; }
        public double Score { get; set; }
        public DateTime CompletionDate { get; set; }
        public List<AnswerDetail> Answers { get; set; }
    }
}
