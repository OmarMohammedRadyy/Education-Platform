using System.ComponentModel.DataAnnotations;

namespace EducationPlatformN.Models
{
    public class Lesson
    {
        public int LessonId { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; }
        public virtual ICollection<LessonNote> LessonNotes { get; set; } = new List<LessonNote>();
        public string VideoUrl { get; set; }
        public int Duration { get; set; } // تغيير من string إلى int
        public Course Course { get; set; }
        public List<Question> Questions { get; set; } = new List<Question>();
        public List<QuizResult> QuizResults { get; set; } = new List<QuizResult>();
        public List<StudentProgress> StudentProgress { get; set; } = new List<StudentProgress>();
    }
}