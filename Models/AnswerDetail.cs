namespace EducationPlatformN.Models
{
    public class AnswerDetail
    {
        public int LessonId { get; set; }
        public string QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string SelectedAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
    }
}
