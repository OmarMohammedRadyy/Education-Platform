using EducationPlatformN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace EducationPlatformN.Controllers
{
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IActionResult Index()
        {
            // التحقق من تسجيل الدخول باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var notifications = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new Notification
                {
                    NotificationId = n.NotificationId,
                    UserId = n.UserId,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    NotificationType = n.NotificationType,
                    InvoiceUrl = ExtractInvoiceUrl(n.Message) // استخدام الدالة الآن كـ static
                })
                .ToList();

            // تحديث حالة القراءة للإشعارات
            foreach (var notification in notifications.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
            }
            _context.SaveChanges();

            // إنشاء PaymentViewModel لتمرير الإشعارات والمعلومات الأخرى
            var viewModel = new PaymentViewModel
            {
                Notifications = notifications,
                IsLoggedIn = true,
                IsAdmin = HttpContext.Session.GetString("Role") == "Admin",
                UnreadNotifications = notifications.Count(n => !n.IsRead),
                Course = null,
                Payments = null,
                ErrorMessage = null,
                SuccessMessage = null
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult MarkAsRead(int id)
        {
            // التحقق من تسجيل الدخول باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return Unauthorized("يجب تسجيل الدخول لإجراء هذا الإجراء.");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var notification = _context.Notifications.Find(id);
            if (notification != null && notification.UserId == userId)
            {
                notification.IsRead = true;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        private static string ExtractInvoiceUrl(string message)
        {
            const string pattern = @"href='([^']*)'";
            var match = Regex.Match(message, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}