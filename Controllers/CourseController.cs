using EducationPlatformN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace EducationPlatformN.Controllers
{
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CourseController> _logger;

        public CourseController(AppDbContext context, ILogger<CourseController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // عرض الدورات المتاحة
        public IActionResult Index()
        {
            try
            {
                int? currentUserId = GetCurrentUserId();
                bool isLoggedIn = currentUserId != null;
                bool isAdmin = isLoggedIn && HttpContext.Session.GetString("Role") == "Admin";

                ViewBag.IsLoggedIn = isLoggedIn;
                ViewBag.IsAdmin = isAdmin;
                ViewBag.UnreadNotifications = isLoggedIn
                    ? _context.Notifications.Count(n => n.UserId == currentUserId && !n.IsRead)
                    : 0;

                var courses = _context.Courses
                    .Include(c => c.Teacher)
                    .ToList();

                return View(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses in Index action.");
                return Content($"خطأ في تحميل الدورات: {ex.Message}");
            }
        }

        // عرض تفاصيل الدورة
        public IActionResult Details(int id)
        {
            try
            {
                var course = _context.Courses
                    .Include(c => c.Teacher)
                    .Include(c => c.Lessons)
                    .ThenInclude(l => l.Questions)
                    .FirstOrDefault(c => c.CourseId == id);

                if (course == null)
                {
                    _logger.LogWarning("Course with ID {CourseId} not found.", id);
                    return NotFound("لم يتم العثور على الدورة.");
                }

                int? currentUserId = GetCurrentUserId();
                bool isLoggedIn = currentUserId != null;
                bool isAdmin = isLoggedIn && HttpContext.Session.GetString("Role") == "Admin";

                ViewBag.IsLoggedIn = isLoggedIn;
                ViewBag.IsAdmin = isAdmin;
                ViewBag.UnreadNotifications = isLoggedIn
                    ? _context.Notifications.Count(n => n.UserId == currentUserId && !n.IsRead)
                    : 0;

                if (isLoggedIn)
                {
                    var certificate = _context.Certificates
                        .FirstOrDefault(c => c.StudentId == currentUserId && c.CourseId == id);
                    ViewBag.CertificateUrl = certificate?.CertificateUrl;
                }
                else
                {
                    ViewBag.CertificateUrl = null;
                }

                return View(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course details for CourseId {CourseId}.", id);
                return Content($"خطأ في تحميل تفاصيل الدورة: {ex.Message}");
            }
        }

        // دالة مساعدة للحصول على معرف المستخدم الحالي من الجلسة
        private int? GetCurrentUserId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                _logger.LogWarning("Session does not contain UserId.");
            }
            return userId;
        }
    }
}