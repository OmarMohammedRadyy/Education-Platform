using EducationPlatformN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;

namespace EducationPlatformN.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IActionResult Index()
        {
            try
            {
                // جلب قائمة الدورات مع معلومات المعلمين
                var courses = _context.Courses
                    .Include(c => c.Teacher)
                    .Select(c => new Course
                    {
                        CourseId = c.CourseId,
                        Title = c.Title ?? "بدون عنوان",
                        Description = c.Description ?? "بدون وصف",
                        Price = c.Price,
                        TeacherId = c.TeacherId,
                        ThumbnailUrl = c.ThumbnailUrl ?? "/images/default-thumbnail.jpg",
                        IntroVideoUrl = c.IntroVideoUrl,
                        CreatedAt = c.CreatedAt,
                        InvoiceUrl = c.InvoiceUrl ?? "",
                        Teacher = c.Teacher != null ? new User
                        {
                            UserId = c.Teacher.UserId,
                            FullName = c.Teacher.FullName ?? "بدون مدرس",
                            Email = c.Teacher.Email ?? "",
                            Phone = c.Teacher.Phone ?? "",
                            PasswordHash = c.Teacher.PasswordHash ?? "",
                            Role = c.Teacher.Role ?? "Unknown",
                            CreatedAt = c.Teacher.CreatedAt,
                            Language = c.Teacher.Language ?? "ar"
                        } : null
                    })
                    .ToList() ?? new List<Course>();

                // تعيين قيمة افتراضية آمنة لـ ViewBag
                bool isLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
                ViewBag.IsLoggedIn = isLoggedIn;
                ViewBag.IsAdmin = isLoggedIn && HttpContext.Session.GetString("Role") == "Admin";
                ViewBag.UnreadNotifications = 0; // يمكن تحديث هذا لاحقًا لاحتواء عدد الإشعارات غير المقروءة

                // تعيين UserId وHasSubscriptions بناءً على حالة تسجيل الدخول
                if (isLoggedIn)
                {
                    int? userId = HttpContext.Session.GetInt32("UserId");
                    if (userId.HasValue)
                    {
                        ViewBag.UserId = userId.Value; // تعيين UserId بشكل دقيق
                        ViewBag.HasSubscriptions = _context.Payments
                            .Any(p => p.UserId == userId.Value && p.Status == "Approved" && p.IsActive);
                    }
                    else
                    {
                        ViewBag.UserId = null; // تعيين قيمة NULL إذا لم يتم العثور على UserId
                        ViewBag.HasSubscriptions = false;
                    }
                }
                else
                {
                    ViewBag.UserId = null; // تعيين قيمة NULL للمستخدمين غير المسجلين
                    ViewBag.HasSubscriptions = false;
                }

                _logger.LogInformation($"Loaded home page with {courses.Count} courses for user {(isLoggedIn ? ViewBag.UserId.ToString() : "anonymous")}.");
                return View(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تحميل صفحة الرئيسية.");
                // تعيين قيمة افتراضية آمنة لتجنب الأخطاء
                ViewBag.IsLoggedIn = false;
                ViewBag.IsAdmin = false;
                ViewBag.UnreadNotifications = 0;
                ViewBag.UserId = null;
                ViewBag.HasSubscriptions = false;
                return View(new List<Course>());
            }
        }
        public IActionResult Privacy()
        {
            // تعيين قيمة افتراضية آمنة لـ ViewBag
            bool isLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
            ViewBag.IsLoggedIn = isLoggedIn;
            ViewBag.IsAdmin = isLoggedIn && HttpContext.Session.GetString("Role") == "Admin";
            ViewBag.UnreadNotifications = 0;
            ViewBag.HasSubscriptions = isLoggedIn && !ViewBag.IsAdmin && _context.Payments.Any(p => p.UserId == HttpContext.Session.GetInt32("UserId") && p.Status == "Approved" && p.IsActive);
            ViewBag.UserId = isLoggedIn ? HttpContext.Session.GetInt32("UserId") : null;

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}