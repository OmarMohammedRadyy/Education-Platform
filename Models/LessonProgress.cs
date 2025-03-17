namespace EducationPlatformN.Models
{
    public class LessonProgress
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; }
        public double Score { get; set; }
        public int AttemptCount { get; set; }
    }
}
