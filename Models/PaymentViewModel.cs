namespace EducationPlatformN.Models
{
    public class PaymentViewModel
    {
        public Course Course { get; set; }
        public List<Payment> Payments { get; set; } = new List<Payment>(); // تهيئة افتراضية لتجنب null
        public List<Notification> Notifications { get; set; } = new List<Notification>(); // تهيئة افتراضية
        public bool IsLoggedIn { get; set; }
        public bool IsAdmin { get; set; }
        public int UnreadNotifications { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
    }
}