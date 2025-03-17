namespace EducationPlatformN.Models
{
    public class CertificateViewModel
    {
        public List<CertificateDisplayModel> Certificates { get; set; }
        public bool IsLoggedIn { get; set; }
        public bool IsAdmin { get; set; }
        public int UnreadNotifications { get; set; }
    }
}
