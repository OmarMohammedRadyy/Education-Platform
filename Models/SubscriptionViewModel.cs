namespace EducationPlatformN.Models
{
    public class SubscriptionViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string TeacherName { get; set; }
        public string ThumbnailUrl { get; set; }
        public string InvoiceUrl { get; set; }
        public float CompletionPercentage { get; set; }
    }
}
