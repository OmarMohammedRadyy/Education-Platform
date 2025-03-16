using System;

namespace EducationPlatformN.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string NotificationType { get; set; } // جديد: لتمييز نوع الإشعار
        public virtual User User { get; set; } // العلاقة مع User
        public string InvoiceUrl { get; set; } // إضافة InvoiceUrl
    }
}