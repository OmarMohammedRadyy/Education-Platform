using System.ComponentModel.DataAnnotations;

namespace EducationPlatformN.Models
{
    public class LessonNote
    {
        public int LessonNoteId { get; set; }
        public int LessonId { get; set; }
        public string FileUrl { get; set; }
        public DateTime UploadDate { get; set; }
        public int UploadedByUserId { get; set; }
        public virtual Lesson Lesson { get; set; }
        public virtual User UploadedBy { get; set; }
    }
}
