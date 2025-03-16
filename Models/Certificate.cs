namespace EducationPlatformN.Models
{
    public class Certificate
    {
        public int CertificateId { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public DateTime IssuedDate { get; set; }
        public string CertificateUrl { get; set; }
    }
}
