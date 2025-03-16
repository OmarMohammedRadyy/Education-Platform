using EducationPlatformN.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EducationPlatformN.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHostEnvironment _env;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(AppDbContext context, IHostEnvironment env, ILogger<PaymentController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // عرض صفحة تأكيد الدفع (GET)
        public IActionResult Submit(int courseId)
        {
            if (!IsUserLoggedIn())
            {
                _logger.LogWarning("User not logged in. Redirecting to Login.");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var course = _context.Courses
                    .Include(c => c.Teacher)
                    .FirstOrDefault(c => c.CourseId == courseId);

                if (course == null)
                {
                    _logger.LogError($"Course with ID {courseId} not found.");
                    return NotFound("الدورة غير متوفرة.");
                }

                var viewModel = CreateViewModel(course: course);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in Submit (GET) for Course ID {courseId}");
                var viewModel = CreateViewModel(error: "حدث خطأ غير متوقع");
                return View(viewModel);
            }
        }

        // معالجة تأكيد الدفع (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int courseId, IFormFile proofImage)
        {
            if (!IsUserLoggedIn())
            {
                _logger.LogWarning("User not logged in. Redirecting to Login.");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var course = _context.Courses
                    .Include(c => c.Teacher)
                    .FirstOrDefault(c => c.CourseId == courseId);

                if (course == null)
                {
                    _logger.LogError($"Course with ID {courseId} not found.");
                    return NotFound("الدورة غير متوفرة.");
                }

                int? userId = GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning("UserId not found in session.");
                    return RedirectToAction("Login", "Account");
                }

                if (_context.Payments.Any(p => p.UserId == userId && p.CourseId == courseId && p.Status == "Approved" && p.IsActive))
                {
                    _logger.LogWarning($"User {userId} already subscribed to Course {courseId}.");
                    var viewModel = CreateViewModel(course: course, error: "أنت بالفعل مشترك في هذه الدورة.");
                    return View(viewModel);
                }

                if (proofImage == null || proofImage.Length == 0)
                {
                    _logger.LogWarning("No proof image provided.");
                    var viewModel = CreateViewModel(course: course, error: "يجب رفع صورة التحويل.");
                    return View(viewModel);
                }

                var allowedExtensions = new[] { ".jpg", ".png", ".jpeg" };
                var extension = Path.GetExtension(proofImage.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning($"Invalid file extension: {extension}");
                    var viewModel = CreateViewModel(course: course, error: "نوع الملف غير مدعوم");
                    return View(viewModel);
                }

                if (proofImage.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning($"File size exceeds limit: {proofImage.Length} bytes");
                    var viewModel = CreateViewModel(course: course, error: "حجم الملف يتجاوز الحد المسموح (5MB)");
                    return View(viewModel);
                }

                string paymentProofUrl = null;
                var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads", "payments");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{userId}_{DateTime.UtcNow.Ticks}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await proofImage.CopyToAsync(stream);
                }
                paymentProofUrl = $"/uploads/payments/{fileName}";

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    var payment = new Payment
                    {
                        CourseId = courseId,
                        UserId = userId.Value,
                        Amount = course.Price,
                        CreatedAt = DateTime.UtcNow,
                        PaymentProofUrl = paymentProofUrl,
                        Status = "Pending",
                        RejectionReason = "",
                        InvoiceUrl = ""
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    var studentNotification = new Notification
                    {
                        UserId = userId.Value,
                        Message = $"تم تقديم طلب دفع لدورة {course.Title}. في انتظار المراجعة.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        NotificationType = "PaymentSubmitted",
                        InvoiceUrl = ""
                    };
                    _context.Notifications.Add(studentNotification);

                    var adminIds = await _context.Users
                        .Where(u => u.Role == "Admin")
                        .Select(u => u.UserId)
                        .ToListAsync();

                    var adminNotifications = adminIds.Select(adminId => new Notification
                    {
                        UserId = adminId,
                        Message = $"طلب دفع جديد من الطالب {userId} لدورة {course.Title}. يرجى المراجعة.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        NotificationType = "PaymentSubmitted",
                        InvoiceUrl = ""
                    }).ToList();

                    _context.Notifications.AddRange(adminNotifications);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation($"Payment submitted successfully for User {userId} and Course {courseId}.");
                    var viewModel = CreateViewModel(course: course, success: "تم تقديم طلب الدفع بنجاح. انتظر الموافقة.");
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Submit (POST)");
                var viewModel = CreateViewModel(error: "حدث خطأ غير متوقع أثناء تسجيل الدفع");
                return View(viewModel);
            }
        }

        // عرض صفحة الاشتراكات
        public IActionResult MySubscriptions()
        {
            if (!IsUserLoggedIn())
            {
                _logger.LogWarning("User not logged in. Redirecting to Login.");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                int? userId = GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning("UserId not found in session.");
                    return RedirectToAction("Login", "Account");
                }

                var subscriptions = _context.Payments
                    .Include(p => p.Course)
                    .ThenInclude(c => c.Teacher)
                    .Include(p => p.Course.Lessons)
                    .ThenInclude(l => l.StudentProgress.Where(sp => sp.StudentId == userId))
                    .Where(p => p.UserId == userId && p.Status == "Approved" && p.IsActive)
                    .Select(p => new SubscriptionViewModel
                    {
                        CourseId = p.Course.CourseId,
                        Title = p.Course.Title ?? "بدون عنوان",
                        Price = p.Course.Price,
                        TeacherName = p.Course.Teacher != null ? p.Course.Teacher.FullName : "بدون مدرس",
                        ThumbnailUrl = p.Course.ThumbnailUrl,
                        InvoiceUrl = p.InvoiceUrl,
                        CompletionPercentage = p.Course.Lessons.Any()
                            ? (p.Course.Lessons.Count(l => l.StudentProgress.Any(sp => sp.IsCompleted)) / (float)p.Course.Lessons.Count) * 100
                            : 0
                    })
                    .ToList();

                ViewBag.IsLoggedIn = true;
                ViewBag.IsAdmin = HttpContext.Session.GetString("Role") == "Admin";
                ViewBag.UnreadNotifications = _context.Notifications
                    .Count(n => n.UserId == userId && !n.IsRead);
                ViewBag.UserId = userId;
                ViewBag.HasSubscriptions = subscriptions.Any();

                _logger.LogInformation($"User {userId} has {subscriptions.Count} active subscriptions.");
                return View(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading MySubscriptions.");
                ViewBag.IsLoggedIn = true;
                ViewBag.IsAdmin = false;
                ViewBag.UnreadNotifications = 0;
                ViewBag.UserId = GetCurrentUserId();
                ViewBag.HasSubscriptions = false;
                return View(new List<SubscriptionViewModel>());
            }
        }

        // عرض دروس الدورة
        public IActionResult MyLessons(int? courseId)
        {
            if (!IsUserLoggedIn())
            {
                _logger.LogWarning("User not logged in. Redirecting to Login.");
                return RedirectToAction("Login", "Account");
            }

            if (!courseId.HasValue || courseId <= 0)
            {
                _logger.LogWarning("Invalid or missing courseId: {courseId}", courseId);
                return BadRequest("معرف الدورة غير صالح.");
            }

            try
            {
                int? userId = GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning("UserId not found in session.");
                    return RedirectToAction("Login", "Account");
                }

                var payment = _context.Payments
                    .Include(p => p.Course)
                    .ThenInclude(c => c.Lessons)
                    .ThenInclude(l => l.Questions)
                    .FirstOrDefault(p => p.UserId == userId && p.CourseId == courseId.Value && p.Status == "Approved" && p.IsActive);

                if (payment == null)
                {
                    _logger.LogWarning($"User {userId} is not subscribed to Course {courseId}.");
                    return Content("أنت لست مشتركًا في هذه الدورة.");
                }

                var course = payment.Course;
                if (course == null)
                {
                    _logger.LogError($"Course {courseId} is missing for Payment {payment.PaymentId}.");
                    return NotFound("لم يتم العثور على بيانات الدورة.");
                }

                ViewBag.IsLoggedIn = true;
                ViewBag.IsAdmin = HttpContext.Session.GetString("Role") == "Admin";
                ViewBag.UnreadNotifications = _context.Notifications
                    .Count(n => n.UserId == userId && !n.IsRead);
                ViewBag.UserId = userId;
                ViewBag.HasSubscriptions = _context.Payments.Any(p => p.UserId == userId && p.Status == "Approved" && p.IsActive);

                _logger.LogInformation($"User {userId} accessed lessons for Course {courseId}.");
                return View(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading MyLessons.");
                ViewBag.IsLoggedIn = true;
                ViewBag.IsAdmin = false;
                ViewBag.UnreadNotifications = 0;
                ViewBag.UserId = GetCurrentUserId();
                ViewBag.HasSubscriptions = false;
                return View(new Course());
            }
        }

        // وضع علامة اكتمال الدرس
        [HttpPost]
        public IActionResult MarkLessonCompleted(int lessonId, int courseId)
        {
            if (!IsUserLoggedIn())
            {
                _logger.LogWarning("User not logged in.");
                return Unauthorized();
            }

            try
            {
                int? studentId = GetCurrentUserId();
                if (studentId == null)
                {
                    _logger.LogWarning("UserId not found in session.");
                    return Unauthorized();
                }

                var progress = _context.StudentProgress
                    .FirstOrDefault(p => p.StudentId == studentId && p.LessonId == lessonId);

                if (progress == null)
                {
                    progress = new StudentProgress
                    {
                        StudentId = studentId.Value,
                        LessonId = lessonId,
                        IsCompleted = true,
                        CompletionDate = DateTime.Now
                    };
                    _context.StudentProgress.Add(progress);
                }
                else
                {
                    progress.IsCompleted = true;
                    progress.CompletionDate = DateTime.Now;
                }
                _context.SaveChanges();

                var certificate = IssueCertificate(studentId.Value, courseId);
                return Json(new { success = true, certificateUrl = certificate?.CertificateUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking lesson {lessonId} as completed.");
                return Json(new { success = false, error = "حدث خطأ أثناء تسجيل التقدم" });
            }
        }

        // إصدار الشهادة
        private Certificate IssueCertificate(int studentId, int courseId)
        {
            var course = _context.Courses
                .Include(c => c.Lessons)
                .Include(c => c.CertificateConditions)
                .FirstOrDefault(c => c.CourseId == courseId);
            var progress = _context.StudentProgress
                .Where(p => p.StudentId == studentId && p.Lesson.CourseId == courseId)
                .ToList();
            var quizResults = _context.QuizResults
                .Where(q => q.UserId == studentId && q.Lesson.CourseId == courseId)
                .ToList();

            bool isEligible = true;
            foreach (var condition in course.CertificateConditions)
            {
                if (condition.ConditionType == "CompletionPercentage")
                {
                    var completedLessons = progress.Count(p => p.IsCompleted);
                    var totalLessons = course.Lessons.Count;
                    var percentage = (completedLessons / (float)totalLessons) * 100;
                    if (percentage < (float)condition.Value)
                        isEligible = false;
                }
                else if (condition.ConditionType == "QuizScore")
                {
                    var avgScore = quizResults.Any() ? quizResults.Average(q => q.Score) : 0;
                    if ((decimal)avgScore < condition.Value)
                        isEligible = false;
                }
            }

            if (isEligible)
            {
                var certificate = new Certificate
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    IssuedDate = DateTime.Now,
                    CertificateUrl = GenerateCertificatePdf(studentId, courseId)
                };
                _context.Certificates.Add(certificate);

                var notification = new Notification
                {
                    UserId = studentId,
                    Message = $"تم إصدار شهادتك لدورة {course.Title} بنجاح!",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    InvoiceUrl = certificate.CertificateUrl
                };
                _context.Notifications.Add(notification);

                _context.SaveChanges();
                return certificate;
            }
            return null;
        }

        // إنشاء ملف PDF للشهادة
        private string GenerateCertificatePdf(int studentId, int courseId)
        {
            var student = _context.Users.Find(studentId);
            var course = _context.Courses.Find(courseId);
            string filePath = Path.Combine(_env.ContentRootPath, "wwwroot/certificates", $"certificate_{studentId}_{courseId}.pdf");

            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(20).FontFamily("Tajawal"));

                    page.Header()
                        .Text("شهادة إتمام دورة")
                        .SemiBold().FontSize(36).FontColor(QuestPDF.Helpers.Colors.Blue.Medium)
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);
                            column.Item().Text($"الطالب: {student.FullName}").FontSize(24);
                            column.Item().Text($"الدورة: {course.Title}").FontSize(24);
                            column.Item().Text($"تاريخ الإصدار: {DateTime.Now:dd/MM/yyyy}").FontSize(18);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("منصة عمر © 2025")
                        .FontSize(12);
                });
            }).GeneratePdf(filePath);

            return $"/certificates/certificate_{studentId}_{courseId}.pdf";
        }

        // إنشاء نموذج العرض
        private PaymentViewModel CreateViewModel(Course course = null, string error = null, string success = null)
        {
            int? userId = GetCurrentUserId();
            return new PaymentViewModel
            {
                Course = course,
                Payments = new List<Payment>(),
                Notifications = new List<Notification>(),
                IsLoggedIn = IsUserLoggedIn(),
                IsAdmin = HttpContext.Session.GetString("Role") == "Admin",
                UnreadNotifications = userId.HasValue
                    ? _context.Notifications.Count(n => n.UserId == userId && !n.IsRead)
                    : 0,
                ErrorMessage = error,
                SuccessMessage = success
            };
        }

        // التحقق من تسجيل الدخول
        private bool IsUserLoggedIn()
        {
            return GetCurrentUserId() != null;
        }

        // الحصول على معرف المستخدم من الجلسة
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