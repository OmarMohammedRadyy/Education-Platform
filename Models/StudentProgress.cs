namespace EducationPlatformN.Models
{
    public class StudentProgress
    {
        public int ProgressId { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; }
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CompletionDate { get; set; }
    }
}
