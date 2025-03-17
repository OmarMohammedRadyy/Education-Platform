namespace EducationPlatformN.Models
{
    public class QuizAttempt
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; }
        public double Score { get; set; }
        public DateTime CompletionDate { get; set; }
    }
}
