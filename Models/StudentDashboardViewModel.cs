using static EducationPlatformN.Controllers.QuizController;

namespace EducationPlatformN.Models
{
    public class StudentDashboardViewModel
    {
        public double CourseProgress { get; set; }
        public List<LessonProgress> Lessons { get; set; }
        public List<QuizAttempt> QuizHistory { get; set; }
        public List<CertificateDisplayModel> Certificates { get; set; }
        public List<LessonNote> Notes { get; set; }
        public string FilterLessonTitle { get; set; }
        public DateTime? FilterStartDate { get; set; }
        public DateTime? FilterEndDate { get; set; }
        public double? FilterMinScore { get; set; }
        public string Message { get; set; } // إضافة حقل الرسالة
    }
}
