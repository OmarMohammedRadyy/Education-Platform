namespace EducationPlatformN.Models
{
    public class QuizResult
    {
        public int QuizResultId { get; set; }
        public int UserId { get; set; } // يتطابق مع نوع UserId في User
        public int LessonId { get; set; }
        public double Score { get; set; }
        public DateTime CompletionDate { get; set; }
        public string AnswersJson { get; set; }
        public User User { get; set; }
        public Lesson Lesson { get; set; }
    }
}
