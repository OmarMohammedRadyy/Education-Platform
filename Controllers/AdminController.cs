using BCrypt.Net;
using EducationPlatformN.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;




namespace EducationPlatformN.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHostEnvironment _env; // استبدل IWebHostEnvironment بـ IHostEnvironment
        private readonly ILogger<AdminController> _logger; // إضافة ILogger للتسجيل

        public AdminController(AppDbContext context, IHostEnvironment env, ILogger<AdminController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult TestConnection()
        {
            try
            {
                var users = _context.Users.ToList();
                return Content($"تم الاتصال بنجاح. عدد المستخدمين: {users.Count}");
            }
            catch (Exception ex)
            {
                return Content($"خطأ في الاتصال: {ex.Message}");
            }
        }

        public IActionResult TestSave()
        {
            try
            {
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == "test@example.com");
                if (existingUser != null)
                {
                    return Content("المستخدم موجود بالفعل. حاول بـ Email مختلف.");
                }

                var testUser = new User
                {
                    FullName = "اختبار",
                    Email = "test2@example.com", // استخدام Email مختلف
                    Phone = "1234567890",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("test123"),
                    Role = "Student",
                    CreatedAt = DateTime.Now,
                    Language = "ar"
                };
                _context.Users.Add(testUser);
                int changes = _context.SaveChanges();
                return Content($"تم حفظ {changes} تغييرات.");
            }
            catch (Exception ex)
            {
                return Content($"خطأ في الاختبار: {ex.Message} - {ex.InnerException?.Message}");
            }
        }

        // لوحة التحكم الرئيسية
        public IActionResult Dashboard()
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية للوصول إلى لوحة التحكم.");
            }

            try
            {
                var model = new AdminDashboardViewModel
                {
                    Users = _context.Users
                        .Include(u => u.Notifications) // تحميل Notifications
                        .Select(u => new User
                        {
                            UserId = u.UserId,
                            FullName = u.FullName ?? "بدون اسم",
                            Email = u.Email ?? "",
                            Phone = u.Phone ?? "",
                            PasswordHash = u.PasswordHash ?? "",
                            Role = u.Role ?? "Unknown",
                            CreatedAt = u.CreatedAt,
                            Language = u.Language ?? "ar",
                            Courses = u.Courses,
                            Payments = u.Payments,
                            UserAnswers = u.UserAnswers,
                            Certificates = u.Certificates,
                            Notifications = u.Notifications.Select(n => new Notification
                            {
                                NotificationId = n.NotificationId,
                                UserId = n.UserId,
                                Message = n.Message,
                                IsRead = n.IsRead,
                                CreatedAt = n.CreatedAt,
                                NotificationType = n.NotificationType,
                                InvoiceUrl = n.InvoiceUrl ?? "" // التعامل مع NULL
                            }).ToList()
                        })
                        .ToList() ?? new List<User>(),
                    Courses = _context.Courses
                        .Include(c => c.Teacher)
                        .Include(c => c.Lessons)
                        .ThenInclude(l => l.Questions)
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
                            InvoiceUrl = c.InvoiceUrl ?? "", // التعامل مع NULL
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
                            } : null,
                            Lessons = c.Lessons != null ? c.Lessons.Select(l => new Lesson
                            {
                                LessonId = l.LessonId,
                                CourseId = l.CourseId,
                                Title = l.Title ?? "بدون عنوان",
                                VideoUrl = l.VideoUrl ?? "",
                                Duration = l.Duration,
                                Questions = l.Questions != null ? l.Questions.Select(q => new Question
                                {
                                    QuestionId = q.QuestionId,
                                    LessonId = q.LessonId,
                                    QuestionText = q.QuestionText ?? "بدون نص",
                                    CorrectAnswer = q.CorrectAnswer ?? "بدون إجابة",
                                    OptionA = q.OptionA ?? "خيار 1",
                                    OptionB = q.OptionB ?? "خيار 2",
                                    OptionC = q.OptionC ?? "خيار 3",
                                    OptionD = q.OptionD ?? "خيار 4"
                                }).ToList() : new List<Question>()
                            }).ToList() : new List<Lesson>(),
                            Payments = c.Payments,
                            Certificates = c.Certificates
                        })
                        .ToList() ?? new List<Course>(),
                    Payments = _context.Payments
                        .Include(p => p.User)
                        .Include(p => p.Course)
                        .Select(p => new Payment
                        {
                            PaymentId = p.PaymentId,
                            UserId = p.UserId,
                            CourseId = p.CourseId,
                            Amount = p.Amount,
                            PaymentProofUrl = p.PaymentProofUrl ?? "",
                            Status = p.Status ?? "Pending",
                            RejectionReason = p.RejectionReason ?? "",
                            CreatedAt = p.CreatedAt,
                            InvoiceUrl = p.InvoiceUrl ?? "", // التعامل مع NULL
                            IsActive = p.IsActive,
                            LastAccessed = p.LastAccessed,
                            User = p.User != null ? new User
                            {
                                UserId = p.User.UserId,
                                FullName = p.User.FullName ?? "بدون اسم",
                                Email = p.User.Email ?? "",
                                Phone = p.User.Phone ?? "",
                                PasswordHash = p.User.PasswordHash ?? "",
                                Role = p.User.Role ?? "Unknown",
                                CreatedAt = p.User.CreatedAt,
                                Language = p.User.Language ?? "ar"
                            } : null,
                            Course = p.Course != null ? new Course
                            {
                                CourseId = p.Course.CourseId,
                                Title = p.Course.Title ?? "بدون عنوان",
                                Description = p.Course.Description ?? "بدون وصف",
                                Price = p.Course.Price,
                                TeacherId = p.Course.TeacherId,
                                ThumbnailUrl = p.Course.ThumbnailUrl ?? "/images/default-thumbnail.jpg",
                                IntroVideoUrl = p.Course.IntroVideoUrl,
                                CreatedAt = p.Course.CreatedAt,
                                InvoiceUrl = p.Course.InvoiceUrl ?? "" // التعامل مع NULL
                            } : null
                        })
                        .ToList() ?? new List<Payment>()
                };

                // تعيين قيمة افتراضية آمنة لـ ViewBag
                ViewBag.IsLoggedIn = true;
                ViewBag.IsAdmin = true;
                ViewBag.UnreadNotifications = 0;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return Content($"خطأ في تحميل لوحة التحكم: {ex.Message} - {ex.StackTrace}");
            }
        }

        // ... (الدوال الأخرى كما هي)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(User user)
        {
            _logger.LogInformation($"Received request for AddUser. Request Method: {HttpContext.Request.Method}, Path: {HttpContext.Request.Path}, QueryString: {HttpContext.Request.QueryString}, Headers: {JsonConvert.SerializeObject(HttpContext.Request.Headers)}");

            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                _logger.LogWarning("User not logged in or session expired.");
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                _logger.LogWarning("User does not have Admin role.");
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            _logger.LogInformation($"Attempting to add user: {JsonConvert.SerializeObject(user)}");

            if (ModelState.IsValid)
            {
                try
                {
                    // التأكد من تشفير كلمة المرور باستخدام BCrypt مع Salt تلقائي
                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        string salt = BCrypt.Net.BCrypt.GenerateSalt(); // توليد Salt افتراضي دون enhancedEntropy
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash, salt, enhancedEntropy: false, hashType: HashType.SHA256);
                    }
                    else
                    {
                        ViewBag.SaveError = "كلمة المرور مطلوبة.";
                        _logger.LogWarning("PasswordHash is null or empty for new user.");
                        return View(user);
                    }

                    user.CreatedAt = DateTime.Now;
                    user.Language = user.Language ?? "ar"; // ضمان تعيين قيمة افتراضية

                    _context.Users.Add(user);
                    int changes = _context.SaveChanges();

                    if (changes > 0)
                    {
                        _logger.LogInformation($"User added successfully. UserId: {user.UserId}");
                        return RedirectToAction("Dashboard");
                    }

                    _logger.LogWarning($"No changes saved for adding user. Checking context state: {JsonConvert.SerializeObject(_context.ChangeTracker.Entries<User>().Select(e => new { e.Entity.UserId, e.State }).ToList())}");
                    ViewBag.SaveError = "لم يتم حفظ المستخدم في قاعدة البيانات. الرجاء التحقق من الاتصال بقاعدة البيانات أو البيانات.";
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, $"Database error while adding user: {ex.Message} - {ex.InnerException?.Message}");
                    ViewBag.SaveError = $"خطأ في قاعدة البيانات أثناء إضافة المستخدم: {ex.Message}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error adding user: {ex.Message} - {ex.StackTrace}");
                    ViewBag.SaveError = $"خطأ أثناء إضافة المستخدم: {ex.Message}";
                }
            }
            else
            {
                _logger.LogWarning($"ModelState is invalid for adding user. Errors: {string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                ViewBag.Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            }

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return View(user);
        }

        public IActionResult EditUser(int id)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية للوصول إلى هذه الصفحة.");
            }

            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return View(user);
        }

        [HttpPost]
        public IActionResult EditUser(User user)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = _context.Users.Find(user.UserId);
                    if (existingUser == null) return NotFound();

                    existingUser.FullName = user.FullName;
                    existingUser.Email = user.Email;
                    existingUser.Phone = user.Phone;
                    existingUser.Role = user.Role;
                    existingUser.Language = user.Language ?? existingUser.Language; // الحفاظ على القيمة الحالية إذا لم يتم إرسالها

                    // التعامل مع PasswordHash: إذا كان فارغًا أو null، احتفظ بالقيمة الحالية
                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        string salt = BCrypt.Net.BCrypt.GenerateSalt(); // توليد Salt افتراضي
                        existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash, salt, enhancedEntropy: false, hashType: HashType.SHA256);
                    }

                    int changes = _context.SaveChanges();
                    if (changes > 0)
                    {
                        return RedirectToAction("Dashboard");
                    }
                    ViewBag.SaveError = "لم يتم حفظ التعديلات.";
                }
                catch (Exception ex)
                {
                    ViewBag.SaveError = $"خطأ أثناء الحفظ: {ex.Message}";
                }
            }
            ViewBag.Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return View(user);
        }

        public IActionResult DeleteUser(int id)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            var user = _context.Users.Find(id);
            if (user != null)
            {
                try
                {
                    // التحقق من العلاقات قبل الحذف
                    if (_context.Courses.Any(c => c.TeacherId == id) || _context.Payments.Any(p => p.UserId == id))
                    {
                        ViewBag.SaveError = "لا يمكن حذف المستخدم لأنه مرتبط بدورات أو مدفوعات.";
                        return RedirectToAction("Dashboard"); // عودة مباشرة بدلاً من View("Dashboard")
                    }

                    _context.Users.Remove(user);
                    int changes = _context.SaveChanges();
                    if (changes == 0)
                    {
                        ViewBag.SaveError = "لم يتم حذف المستخدم.";
                        return RedirectToAction("Dashboard");
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.SaveError = $"خطأ أثناء الحذف: {ex.Message}";
                    return RedirectToAction("Dashboard");
                }
            }

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return RedirectToAction("Dashboard");
        }

        // إدارة الدورات
        public IActionResult AddCourse()
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية للوصول إلى هذه الصفحة.");
            }

            try
            {
                var teachers = _context.Users
                    .Where(u => u.Role == "Teacher")
                    .Select(u => new TeacherDto { Id = u.UserId, Name = u.FullName })
                    .ToList() ?? new List<TeacherDto>();

                // تعيين قيمة افتراضية آمنة لـ ViewBag
                ViewBag.IsLoggedIn = true;
                ViewBag.IsAdmin = true;
                ViewBag.UnreadNotifications = 0;

                ViewBag.TeachersJson = JsonConvert.SerializeObject(teachers);
                return View();
            }
            catch (Exception ex)
            {
                return Content($"خطأ في تحميل صفحة إضافة الدورة: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult AddCourse(string title, string description, decimal price, int? teacherId, string thumbnailUrl, string introVideoUrl)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            try
            {
                if (string.IsNullOrEmpty(title) || price <= 0)
                {
                    ViewBag.SaveError = "العنوان مطلوب ويجب أن يكون السعر أكبر من 0.";
                    var teacher = _context.Users
                        .Where(u => u.Role == "Teacher")
                        .Select(u => new TeacherDto { Id = u.UserId, Name = u.FullName })
                        .ToList() ?? new List<TeacherDto>();
                    ViewBag.TeachersJson = JsonConvert.SerializeObject(teacher);
                    return View();
                }

                var course = new Course
                {
                    Title = title,
                    Description = description,
                    Price = price,
                    TeacherId = teacherId, // يمكن أن يكون null
                    ThumbnailUrl = string.IsNullOrEmpty(thumbnailUrl) ? null : thumbnailUrl,
                    IntroVideoUrl = string.IsNullOrEmpty(introVideoUrl) ? null : introVideoUrl, // إضافة IntroVideoUrl مع قيمة افتراضية
                    CreatedAt = DateTime.Now
                };
                _context.Courses.Add(course);
                int changes = _context.SaveChanges();
                if (changes > 0)
                {
                    return RedirectToAction("Dashboard");
                }
                ViewBag.SaveError = "لم يتم حفظ الدورة.";
            }
            catch (DbUpdateException ex)
            {
                ViewBag.SaveError = $"خطأ في قاعدة البيانات أثناء الحفظ: {ex.Message} - {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                ViewBag.SaveError = $"خطأ غير متوقع أثناء الحفظ: {ex.Message} - {ex.StackTrace}";
            }

            var teachers = _context.Users
                .Where(u => u.Role == "Teacher")
                .Select(u => new TeacherDto { Id = u.UserId, Name = u.FullName })
                .ToList() ?? new List<TeacherDto>();

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            ViewBag.TeachersJson = JsonConvert.SerializeObject(teachers);
            return View();
        }

        [HttpGet]
        public IActionResult EditCourse(int id)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية للوصول إلى هذه الصفحة.");
            }

            try
            {
                var course = _context.Courses
                    .Include(c => c.Teacher)
                    .FirstOrDefault(c => c.CourseId == id);

                if (course == null)
                {
                    Console.WriteLine($"لم يتم العثور على الدورة بـ CourseId = {id}.");
                    Console.WriteLine($"عدد الدورات في قاعدة البيانات: {_context.Courses.Count()}");
                    return NotFound($"لم يتم العثور على الدورة. تأكد من معرف الدورة (CourseId = {id}).");
                }

                ViewBag.Teachers = _context.Users.Where(u => u.Role == "Teacher").ToList() ?? new List<User>();

                // تعيين قيمة افتراضية آمنة لـ ViewBag
                ViewBag.IsLoggedIn = true;
                ViewBag.IsAdmin = true;
                ViewBag.UnreadNotifications = 0;

                return View(course);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في تحميل صفحة تعديل الدورة: {ex.Message} - {ex.StackTrace}");
                return Content($"خطأ في تحميل صفحة تعديل الدورة: {ex.Message} - {ex.InnerException?.Message}");
            }
        }

        [HttpPost]
        public IActionResult EditCourse(int courseId, string title, string description, decimal price, int? teacherId, string thumbnailUrl)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            try
            {
                var course = _context.Courses
                    .Include(c => c.Teacher)
                    .FirstOrDefault(c => c.CourseId == courseId);
                if (course == null)
                {
                    return NotFound("لم يتم العثور على الدورة.");
                }

                if (string.IsNullOrEmpty(title) || price <= 0)
                {
                    ViewBag.SaveError = "العنوان مطلوب ويجب أن يكون السعر أكبر من 0.";
                    ViewBag.Course = JsonConvert.SerializeObject(course);
                    ViewBag.Teachers = _context.Users.Where(u => u.Role == "Teacher").ToList() ?? new List<User>();
                    return View();
                }

                course.Title = title;
                course.Description = description;
                course.Price = price;
                course.TeacherId = teacherId; // يمكن أن يكون null
                course.ThumbnailUrl = string.IsNullOrEmpty(thumbnailUrl) ? null : thumbnailUrl;

                int changes = _context.SaveChanges();
                if (changes > 0)
                {
                    return RedirectToAction("Dashboard");
                }
                ViewBag.SaveError = "لم يتم حفظ التعديلات. لم يتم تسجيل أي تغييرات في قاعدة البيانات.";
            }
            catch (DbUpdateException ex)
            {
                ViewBag.SaveError = $"خطأ في قاعدة البيانات أثناء الحفظ: {ex.Message} - {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                ViewBag.SaveError = $"خطأ غير متوقع أثناء الحفظ: {ex.Message} - {ex.StackTrace}";
            }

            ViewBag.Course = JsonConvert.SerializeObject(_context.Courses.Find(courseId));
            ViewBag.Teachers = _context.Users.Where(u => u.Role == "Teacher").ToList() ?? new List<User>();

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return View();
        }

        [HttpGet]
        public IActionResult DeleteCourse(int id)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            try
            {
                var course = _context.Courses
                    .Include(c => c.Lessons) // تحميل الدروس المرتبطة
                    .Include(c => c.Payments) // تحميل المدفوعات المرتبطة
                    .Include(c => c.Certificates) // تحميل الشهادات المرتبطة
                    .FirstOrDefault(c => c.CourseId == id);

                if (course == null)
                {
                    ViewBag.SaveError = "لم يتم العثور على الدورة. تأكد من معرف الدورة.";
                    return RedirectToAction("Dashboard");
                }

                if (course.Lessons != null && course.Lessons.Any())
                {
                    _context.Lessons.RemoveRange(course.Lessons);
                }

                if (course.Payments != null && course.Payments.Any())
                {
                    _context.Payments.RemoveRange(course.Payments);
                }

                if (course.Certificates != null && course.Certificates.Any())
                {
                    _context.Certificates.RemoveRange(course.Certificates);
                }

                _context.Courses.Remove(course);
                int changes = _context.SaveChanges();

                if (changes > 0)
                {
                    TempData["SuccessMessage"] = "تم حذف الدورة والعلاقات المرتبطة بنجاح.";
                }
                else
                {
                    ViewBag.SaveError = "لم يتم حذف الدورة. لم يتم تسجيل أي تغييرات في قاعدة البيانات.";
                }
            }
            catch (DbUpdateException ex)
            {
                ViewBag.SaveError = $"خطأ في قاعدة البيانات أثناء الحذف: {ex.Message} - {ex.InnerException?.Message}";
                Console.WriteLine($"DbUpdateException StackTrace: {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                ViewBag.SaveError = $"خطأ غير متوقع أثناء الحذف: {ex.Message} - {ex.StackTrace}";
                Console.WriteLine($"Exception StackTrace: {ex.StackTrace}");
            }

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return RedirectToAction("Dashboard");
        }

        // إضافة دالة ReviewPayments لعرض طلبات الدفع
        public IActionResult ReviewPayments()
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية للوصول إلى هذه الصفحة.");
            }

            try
            {
                var pendingPayments = _context.Payments
                    .Include(p => p.User)
                    .Include(p => p.Course)
                    .Where(p => p.Status == "Pending")
                    .Select(p => new Payment
                    {
                        PaymentId = p.PaymentId,
                        UserId = p.UserId,
                        CourseId = p.CourseId,
                        Amount = p.Amount,
                        PaymentProofUrl = p.PaymentProofUrl ?? "",
                        Status = p.Status ?? "Pending",
                        RejectionReason = p.RejectionReason ?? "",
                        CreatedAt = p.CreatedAt,
                        InvoiceUrl = p.InvoiceUrl ?? "",
                        IsActive = p.IsActive,
                        LastAccessed = p.LastAccessed,
                        User = p.User != null ? new User
                        {
                            UserId = p.User.UserId,
                            FullName = p.User.FullName ?? "بدون اسم",
                            Email = p.User.Email ?? "",
                            Phone = p.User.Phone ?? "",
                            PasswordHash = p.User.PasswordHash ?? "",
                            Role = p.User.Role ?? "Unknown",
                            CreatedAt = p.User.CreatedAt,
                            Language = p.User.Language ?? "ar"
                        } : null,
                        Course = p.Course != null ? new Course
                        {
                            CourseId = p.Course.CourseId,
                            Title = p.Course.Title ?? "بدون عنوان",
                            Description = p.Course.Description ?? "بدون وصف",
                            Price = p.Course.Price,
                            TeacherId = p.Course.TeacherId,
                            ThumbnailUrl = p.Course.ThumbnailUrl ?? "/images/default-thumbnail.jpg",
                            IntroVideoUrl = p.Course.IntroVideoUrl ?? "",
                            CreatedAt = p.Course.CreatedAt,
                            InvoiceUrl = p.Course.InvoiceUrl ?? ""
                        } : null
                    })
                    .ToList() ?? new List<Payment>();

                // تعيين قيمة افتراضية آمنة لـ ViewBag
                ViewBag.IsLoggedIn = true;
                ViewBag.IsAdmin = true;
                ViewBag.UnreadNotifications = 0;

                _logger.LogInformation($"Loaded {pendingPayments.Count} pending payments for review.");
                return View(pendingPayments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل طلبات الدفع.");
                ViewBag.IsLoggedIn = true;
                ViewBag.IsAdmin = true;
                ViewBag.UnreadNotifications = 0;
                return Content($"خطأ في تحميل طلبات الدفع: {ex.Message} - {ex.StackTrace}");
            }
        }

        // إضافة دالة للموافقة على الدفع
        [HttpPost]
        public IActionResult ApprovePayment(int paymentId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            try
            {
                // التحقق من وجود الدفعة باستخدام Select لتجنب NULL
                var paymentQuery = _context.Payments
                    .Include(p => p.User)
                    .Include(p => p.Course)
                    .Where(p => p.PaymentId == paymentId)
                    .Select(p => new Payment
                    {
                        PaymentId = p.PaymentId,
                        UserId = p.UserId,
                        CourseId = p.CourseId,
                        Amount = p.Amount,
                        PaymentProofUrl = p.PaymentProofUrl,
                        Status = p.Status,
                        RejectionReason = p.RejectionReason,
                        CreatedAt = p.CreatedAt,
                        InvoiceUrl = p.InvoiceUrl,
                        IsActive = p.IsActive,
                        LastAccessed = p.LastAccessed,
                        User = p.User != null ? new User
                        {
                            UserId = p.User.UserId,
                            FullName = p.User.FullName,
                            Email = p.User.Email,
                            Phone = p.User.Phone,
                            PasswordHash = p.User.PasswordHash,
                            Role = p.User.Role,
                            CreatedAt = p.User.CreatedAt,
                            Language = p.User.Language
                        } : null,
                        Course = p.Course != null ? new Course
                        {
                            CourseId = p.Course.CourseId,
                            Title = p.Course.Title,
                            Description = p.Course.Description,
                            Price = p.Course.Price,
                            TeacherId = p.Course.TeacherId,
                            ThumbnailUrl = p.Course.ThumbnailUrl,
                            IntroVideoUrl = p.Course.IntroVideoUrl,
                            CreatedAt = p.Course.CreatedAt,
                            InvoiceUrl = p.Course.InvoiceUrl
                        } : null
                    })
                    .FirstOrDefault();

                if (paymentQuery == null || paymentQuery.Status != "Pending")
                {
                    TempData["ErrorMessage"] = "لا يمكن الموافقة على هذا الطلب.";
                    _logger.LogWarning($"Payment {paymentId} not found or not in 'Pending' status. PaymentQuery: {JsonConvert.SerializeObject(paymentQuery)}");
                    return RedirectToAction("ReviewPayments");
                }

                // التحقق من البيانات قبل الاستخدام
                if (paymentQuery.User == null)
                {
                    _logger.LogError($"User for Payment {paymentId} is null or missing. PaymentQuery: {JsonConvert.SerializeObject(paymentQuery)}");
                    TempData["ErrorMessage"] = "بيانات المستخدم مفقودة للدفعة. الرجاء التحقق من قاعدة البيانات.";
                    return RedirectToAction("ReviewPayments");
                }

                if (paymentQuery.Course == null)
                {
                    _logger.LogError($"Course for Payment {paymentId} is null or missing. PaymentQuery: {JsonConvert.SerializeObject(paymentQuery)}");
                    TempData["ErrorMessage"] = "بيانات الدورة مفقودة للدفعة. الرجاء التحقق من قاعدة البيانات.";
                    return RedirectToAction("ReviewPayments");
                }

                // التحقق من أن الدفعة موجودة في السياق لتجنب مشكلات التتبع
                var paymentInContext = _context.Payments
                    .Include(p => p.User)
                    .Include(p => p.Course)
                    .FirstOrDefault(p => p.PaymentId == paymentId);

                if (paymentInContext == null)
                {
                    _logger.LogError($"Payment {paymentId} not found in context after initial query.");
                    TempData["ErrorMessage"] = "لم يتم العثور على بيانات الدفعة في قاعدة البيانات.";
                    return RedirectToAction("ReviewPayments");
                }

                // تحديث البيانات مباشرة في السياق
                paymentInContext.Status = "Approved";
                paymentInContext.InvoiceUrl = GenerateInvoice(paymentInContext.UserId, paymentInContext.CourseId, paymentInContext.PaymentId);
                paymentInContext.IsActive = true;
                paymentInContext.LastAccessed = DateTime.Now;

                // تسجيل الحالة قبل الحفظ
                _logger.LogInformation($"Attempting to save changes for Payment {paymentId}. Current state: Status={paymentInContext.Status}, IsActive={paymentInContext.IsActive}, LastAccessed={paymentInContext.LastAccessed}, InvoiceUrl={paymentInContext.InvoiceUrl}");

                int changes = _context.SaveChanges();
                if (changes == 0)
                {
                    _logger.LogWarning($"No changes saved for Payment {paymentId} approval. Checking context state: {JsonConvert.SerializeObject(_context.ChangeTracker.Entries<Payment>().Select(e => new { e.Entity.PaymentId, e.State }).ToList())}");
                    TempData["ErrorMessage"] = "لم يتم حفظ التغييرات في قاعدة البيانات. الرجاء التحقق من الاتصال بقاعدة البيانات أو البيانات.";
                    return RedirectToAction("ReviewPayments");
                }

                // إرسال إشعار للطالب
                _context.Notifications.Add(new Notification
                {
                    UserId = paymentInContext.UserId,
                    Message = $"تهانينا! تمت الموافقة على طلب دفعك لدورة {paymentInContext.Course.Title}.",
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    NotificationType = "PaymentApproved",
                    InvoiceUrl = paymentInContext.InvoiceUrl
                });

                changes = _context.SaveChanges();
                if (changes == 0)
                {
                    _logger.LogWarning($"No changes saved for notification of Payment {paymentId} approval.");
                    TempData["ErrorMessage"] = "لم يتم حفظ إشعار الموافقة في قاعدة البيانات.";
                }
                else
                {
                    _logger.LogInformation($"Notification for payment approval saved with {changes} changes for user {paymentInContext.UserId}.");
                }

                TempData["SuccessMessage"] = "تمت الموافقة على الدفع بنجاح.";
                return RedirectToAction("ReviewPayments");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Database error approving payment {paymentId}: {ex.Message} - {ex.InnerException?.Message}");
                TempData["ErrorMessage"] = $"خطأ في قاعدة البيانات أثناء الموافقة على الدفع: {ex.Message}";
                return RedirectToAction("ReviewPayments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving payment {paymentId}: {ex.Message} - {ex.StackTrace}");
                TempData["ErrorMessage"] = $"خطأ أثناء الموافقة على الدفع: {ex.Message}";
                return RedirectToAction("ReviewPayments");
            }
        }
        private string GenerateInvoice(int userId, int courseId, int paymentId)
        {
            // تكوين ترخيص QuestPDF (Community License للاستخدام المجاني)
            QuestPDF.Settings.License = LicenseType.Community;

            var user = _context.Users
                .Select(u => new User
                {
                    UserId = u.UserId,
                    FullName = u.FullName ?? "بدون اسم",
                    Email = u.Email ?? "",
                    Phone = u.Phone ?? "",
                    PasswordHash = u.PasswordHash ?? "",
                    Role = u.Role ?? "Unknown",
                    CreatedAt = u.CreatedAt,
                    Language = u.Language ?? "ar"
                })
                .FirstOrDefault(u => u.UserId == userId);

            var course = _context.Courses
                .Select(c => new Course
                {
                    CourseId = c.CourseId,
                    Title = c.Title ?? "بدون عنوان",
                    Description = c.Description ?? "بدون وصف",
                    Price = c.Price,
                    TeacherId = c.TeacherId,
                    ThumbnailUrl = c.ThumbnailUrl ?? "/images/default-thumbnail.jpg",
                    IntroVideoUrl = c.IntroVideoUrl ?? "",
                    CreatedAt = c.CreatedAt,
                    InvoiceUrl = c.InvoiceUrl ?? ""
                })
                .FirstOrDefault(c => c.CourseId == courseId);

            var payment = _context.Payments
                .Select(p => new Payment
                {
                    PaymentId = p.PaymentId,
                    UserId = p.UserId,
                    CourseId = p.CourseId,
                    Amount = p.Amount,
                    PaymentProofUrl = p.PaymentProofUrl ?? "",
                    Status = p.Status ?? "Pending",
                    RejectionReason = p.RejectionReason ?? "",
                    CreatedAt = p.CreatedAt,
                    InvoiceUrl = p.InvoiceUrl ?? "",
                    IsActive = p.IsActive,
                    LastAccessed = p.LastAccessed
                })
                .FirstOrDefault(p => p.PaymentId == paymentId);

            if (user == null || course == null || payment == null)
            {
                throw new InvalidOperationException("لا يمكن إنشاء الفاتورة: بيانات المستخدم، الدورة، أو الدفع مفقودة.");
            }

            var fileName = $"invoice_{paymentId}_{DateTime.Now.Ticks}.pdf";
            var directoryPath = Path.Combine(_env.ContentRootPath, "invoices");
            var filePath = Path.Combine(directoryPath, fileName);

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // استخدام QuestPDF لتوليد الفاتورة
                var document = Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(12));

                        // دمج Header في تعريف واحد باستخدام Column
                        page.Header()
                            .PaddingTop(1, Unit.Centimetre)
                            .Column(x =>
                            {
                                x.Item().Text("منصة عمر")
                                    .SemiBold().FontSize(24).FontColor(Colors.Black)
                                    .AlignCenter();

                                x.Item().PaddingTop(1, Unit.Centimetre) // إضافة مسافة فوق "فاتورة الاشتراك"
                                    .Text("فاتورة الاشتراك")
                                    .SemiBold().FontSize(18).FontColor(Colors.Grey.Darken2)
                                    .AlignCenter();
                            });

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(x =>
                            {
                                x.Item().Text($"رقم الفاتورة: #{payment.PaymentId}")
                                    .FontSize(12);

                                x.Item().Text($"تاريخ الإصدار: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(12);

                                x.Item().Text($"الطالب: {user.FullName}")
                                    .FontSize(12);

                                x.Item().Text($"البريد الإلكتروني: {user.Email}")
                                    .FontSize(12);

                                x.Item().Text($"رقم الهاتف: {user.Phone}")
                                    .FontSize(12);

                                x.Item().Text($"الدورة: {course.Title}")
                                    .FontSize(12);

                                x.Item().Text($"السعر: {payment.Amount.ToString("F2")} ريال") // تحويل decimal إلى نص بـ "F2"
                                    .FontSize(12);

                                x.Item().PaddingTop(2, Unit.Centimetre) // تطبيق PaddingTop على Item كـ IContainer
                                    .Text("رسالة ترحيبية:")
                                    .SemiBold().FontSize(14);

                                x.Item().Text($"نرحب بكم في منصة عمر، ونتمنى لكم نجاحًا في رحلتكم عمرية مع دورة {course.Title}!")
                                    .FontSize(12)
                                    .LineHeight(1);
                            });

                        // دمج Footer في تعريف واحد باستخدام Column
                        page.Footer()
                            .PaddingTop(1, Unit.Centimetre)
                            .Column(x =>
                            {
                                x.Item().AlignCenter()
                                    .Text(y =>
                                    {
                                        y.Span("توقيع المشرف: ")
                                            .FontSize(12)
                                            .SemiBold();
                                        y.Span(HttpContext?.Session?.GetString("FullName") ?? "مشرف النظام")
                                            .FontSize(12);
                                        y.Span($" | تاريخ التوقيع: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                            .FontSize(10);
                                    });

                                x.Item().PaddingTop(1, Unit.Centimetre) // إضافة مسافة فوق "جميع الحقوق محفوظة"
                                    .AlignCenter()
                                    .Text("جميع الحقوق محفوظة © 2025 منصة عمر")
                                    .FontSize(10);
                            });
                    });
                });

                // توليد الملف PDF وتخزينه
                document.GeneratePdf(filePath);

                return $"/invoices/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating invoice for PaymentId {paymentId}");
                throw new InvalidOperationException($"فشل في إنشاء الفاتورة: {ex.Message}", ex);
            }
        }
        // إضافة دالة لرفض الدفع
        [HttpPost]
        public IActionResult RejectPayment(int paymentId, string rejectionReason)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            try
            {
                // التحقق من وجود الدفعة باستخدام Select لتجنب NULL
                var paymentQuery = _context.Payments
                    .Include(p => p.User)
                    .Include(p => p.Course)
                    .Where(p => p.PaymentId == paymentId)
                    .Select(p => new Payment
                    {
                        PaymentId = p.PaymentId,
                        UserId = p.UserId,
                        CourseId = p.CourseId,
                        Amount = p.Amount,
                        PaymentProofUrl = p.PaymentProofUrl,
                        Status = p.Status,
                        RejectionReason = p.RejectionReason,
                        CreatedAt = p.CreatedAt,
                        InvoiceUrl = p.InvoiceUrl,
                        IsActive = p.IsActive,
                        LastAccessed = p.LastAccessed,
                        User = p.User != null ? new User
                        {
                            UserId = p.User.UserId,
                            FullName = p.User.FullName,
                            Email = p.User.Email,
                            Phone = p.User.Phone,
                            PasswordHash = p.User.PasswordHash,
                            Role = p.User.Role,
                            CreatedAt = p.User.CreatedAt,
                            Language = p.User.Language
                        } : null,
                        Course = p.Course != null ? new Course
                        {
                            CourseId = p.Course.CourseId,
                            Title = p.Course.Title,
                            Description = p.Course.Description,
                            Price = p.Course.Price,
                            TeacherId = p.Course.TeacherId,
                            ThumbnailUrl = p.Course.ThumbnailUrl,
                            IntroVideoUrl = p.Course.IntroVideoUrl,
                            CreatedAt = p.Course.CreatedAt,
                            InvoiceUrl = p.Course.InvoiceUrl
                        } : null
                    })
                    .FirstOrDefault();

                if (paymentQuery == null || paymentQuery.Status != "Pending")
                {
                    TempData["ErrorMessage"] = "لا يمكن رفض هذا الطلب.";
                    _logger.LogWarning($"Payment {paymentId} not found or not in 'Pending' status. PaymentQuery: {JsonConvert.SerializeObject(paymentQuery)}");
                    return RedirectToAction("ReviewPayments");
                }

                if (string.IsNullOrEmpty(rejectionReason))
                {
                    TempData["ErrorMessage"] = "يجب توفير سبب الرفض.";
                    return RedirectToAction("ReviewPayments");
                }

                // التحقق من البيانات قبل الاستخدام
                if (paymentQuery.User == null)
                {
                    _logger.LogError($"User for Payment {paymentId} is null or missing. PaymentQuery: {JsonConvert.SerializeObject(paymentQuery)}");
                    TempData["ErrorMessage"] = "بيانات المستخدم مفقودة للدفعة. الرجاء التحقق من قاعدة البيانات.";
                    return RedirectToAction("ReviewPayments");
                }

                if (paymentQuery.Course == null)
                {
                    _logger.LogError($"Course for Payment {paymentId} is null or missing. PaymentQuery: {JsonConvert.SerializeObject(paymentQuery)}");
                    TempData["ErrorMessage"] = "بيانات الدورة مفقودة للدفعة. الرجاء التحقق من قاعدة البيانات.";
                    return RedirectToAction("ReviewPayments");
                }

                // التحقق من أن الدفعة موجودة في السياق لتجنب مشكلات التتبع
                var paymentInContext = _context.Payments
                    .Include(p => p.User)
                    .Include(p => p.Course)
                    .FirstOrDefault(p => p.PaymentId == paymentId);

                if (paymentInContext == null)
                {
                    _logger.LogError($"Payment {paymentId} not found in context after initial query.");
                    TempData["ErrorMessage"] = "لم يتم العثور على بيانات الدفعة في قاعدة البيانات.";
                    return RedirectToAction("ReviewPayments");
                }

                // تحديث البيانات مباشرة في السياق
                paymentInContext.Status = "Rejected";
                paymentInContext.RejectionReason = rejectionReason;
                paymentInContext.IsActive = false;

                // تسجيل الحالة قبل الحفظ
                _logger.LogInformation($"Attempting to save changes for Payment {paymentId}. Current state: Status={paymentInContext.Status}, RejectionReason={paymentInContext.RejectionReason}, IsActive={paymentInContext.IsActive}");

                int changes = _context.SaveChanges();
                if (changes == 0)
                {
                    _logger.LogWarning($"No changes saved for Payment {paymentId} rejection. Checking context state: {JsonConvert.SerializeObject(_context.ChangeTracker.Entries<Payment>().Select(e => new { e.Entity.PaymentId, e.State }).ToList())}");
                    TempData["ErrorMessage"] = "لم يتم حفظ التغييرات في قاعدة البيانات. الرجاء التحقق من الاتصال بقاعدة البيانات أو البيانات.";
                    return RedirectToAction("ReviewPayments");
                }

                // إرسال إشعار للطالب
                _context.Notifications.Add(new Notification
                {
                    UserId = paymentInContext.UserId,
                    Message = $"تم رفض طلب دفعك لدورة {paymentInContext.Course.Title}. السبب: {rejectionReason}",
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    NotificationType = "PaymentRejected"
                });

                changes = _context.SaveChanges();
                if (changes == 0)
                {
                    _logger.LogWarning($"No changes saved for notification of Payment {paymentId} rejection.");
                    TempData["ErrorMessage"] = "لم يتم حفظ إشعار الرفض في قاعدة البيانات.";
                }
                else
                {
                    _logger.LogInformation($"Notification for payment rejection saved with {changes} changes for user {paymentInContext.UserId}.");
                }

                TempData["SuccessMessage"] = "تم رفض الدفع بنجاح.";
                return RedirectToAction("ReviewPayments");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Database error rejecting payment {paymentId}: {ex.Message} - {ex.InnerException?.Message}");
                TempData["ErrorMessage"] = $"خطأ في قاعدة البيانات أثناء رفض الدفع: {ex.Message}";
                return RedirectToAction("ReviewPayments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting payment {paymentId}: {ex.Message} - {ex.StackTrace}");
                TempData["ErrorMessage"] = $"خطأ أثناء رفض الدفع: {ex.Message}";
                return RedirectToAction("ReviewPayments");
            }
        }

        // إدارة الدروس
        [HttpGet]
        public IActionResult AddLesson(int courseId)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية للوصول إلى هذه الصفحة.");
            }

            if (courseId == 0)
            {
                return Content("CourseId is missing or invalid.");
            }
            var course = _context.Courses.Find(courseId);
            if (course == null) return NotFound();

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return View(new Lesson { CourseId = courseId });
        }

        [HttpPost]
        public IActionResult AddLesson(int courseId, string title, string videoUrl, int duration)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            try
            {
                if (courseId <= 0)
                {
                    ViewBag.SaveError = "معرف الدورة غير صالح. يجب أن يكون رقمًا موجبًا.";
                    ViewBag.Errors = new List<string> { "CourseId مطلوب ويجب أن يكون أكبر من 0." };
                    return View(new Lesson { CourseId = courseId });
                }

                var course = _context.Courses.Find(courseId);
                if (course == null)
                {
                    ViewBag.SaveError = "الدورة غير موجودة. تأكد من معرف الدورة.";
                    ViewBag.Errors = new List<string> { "CourseId غير موجود في قاعدة البيانات." };
                    return View(new Lesson { CourseId = courseId });
                }

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(videoUrl) || duration <= 0)
                {
                    ViewBag.SaveError = "عنوان الدرس، رابط الفيديو، والمدة مطلوبة، ويجب أن تكون المدة أكبر من 0.";
                    ViewBag.Errors = new List<string>
                    {
                        string.IsNullOrEmpty(title) ? "عنوان الدرس مطلوب." : "",
                        string.IsNullOrEmpty(videoUrl) ? "رابط الفيديو مطلوب." : "",
                        duration <= 0 ? "المدة يجب أن تكون أكبر من 0." : ""
                    }.Where(e => !string.IsNullOrEmpty(e)).ToList();
                    return View(new Lesson { CourseId = courseId, Title = title, VideoUrl = videoUrl, Duration = duration });
                }

                var lesson = new Lesson
                {
                    CourseId = courseId,
                    Title = title,
                    VideoUrl = videoUrl,
                    Duration = duration,
                    Questions = new List<Question>()
                };

                _context.Lessons.Add(lesson);
                int changes = _context.SaveChanges();
                if (changes > 0)
                {
                    return RedirectToAction("Dashboard");
                }
                ViewBag.SaveError = "لم يتم حفظ الدرس. لم يتم تسجيل أي تغييرات في قاعدة البيانات.";
            }
            catch (DbUpdateException ex)
            {
                ViewBag.SaveError = $"خطأ في قاعدة البيانات أثناء الحفظ: {ex.Message} - {ex.InnerException?.Message}";
                ViewBag.Errors = new List<string> { ex.Message };
            }
            catch (Exception ex)
            {
                ViewBag.SaveError = $"خطأ غير متوقع أثناء الحفظ: {ex.Message} - {ex.StackTrace}";
                ViewBag.Errors = new List<string> { ex.Message };
            }

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return View(new Lesson { CourseId = courseId, Title = title, VideoUrl = videoUrl, Duration = duration });
        }

        [HttpGet]
        public IActionResult EditLesson(int id)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية للوصول إلى هذه الصفحة.");
            }

            try
            {
                var lesson = _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefault(l => l.LessonId == id);

                if (lesson == null)
                {
                    return NotFound("لم يتم العثور على الدرس. تأكد من معرف الدرس.");
                }

                // تعيين قيمة افتراضية آمنة لـ ViewBag
                ViewBag.IsLoggedIn = true;
                ViewBag.IsAdmin = true;
                ViewBag.UnreadNotifications = 0;

                return View(lesson);
            }
            catch (Exception ex)
            {
                return Content($"خطأ في تحميل صفحة تعديل الدرس: {ex.Message} - {ex.StackTrace}");
            }
        }

        [HttpPost]
        public IActionResult EditLesson(int lessonId, int courseId, string title, string videoUrl, int duration)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            try
            {
                if (lessonId <= 0)
                {
                    ViewBag.SaveError = "معرف الدرس غير صالح. يجب أن يكون رقمًا موجبًا.";
                    ViewBag.Errors = new List<string> { "LessonId مطلوب ويجب أن يكون أكبر من 0." };
                    return View(new Lesson());
                }

                if (courseId <= 0)
                {
                    ViewBag.SaveError = "معرف الدورة غير صالح. يجب أن يكون رقمًا موجبًا.";
                    ViewBag.Errors = new List<string> { "CourseId مطلوب ويجب أن يكون أكبر من 0." };
                    return View(new Lesson());
                }

                var course = _context.Courses.Find(courseId);
                if (course == null)
                {
                    ViewBag.SaveError = "الدورة غير موجودة. تأكد من معرف الدورة.";
                    ViewBag.Errors = new List<string> { "CourseId غير موجود في قاعدة البيانات." };
                    return View(new Lesson());
                }

                var lesson = _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefault(l => l.LessonId == lessonId);

                if (lesson == null)
                {
                    return NotFound("لم يتم العثور على الدرس.");
                }

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(videoUrl) || duration <= 0)
                {
                    ViewBag.SaveError = "عنوان الدرس، رابط الفيديو، والمدة مطلوبة، ويجب أن تكون المدة أكبر من 0.";
                    ViewBag.Errors = new List<string>
                    {
                        string.IsNullOrEmpty(title) ? "عنوان الدرس مطلوب." : "",
                        string.IsNullOrEmpty(videoUrl) ? "رابط الفيديو مطلوب." : "",
                        duration <= 0 ? "المدة يجب أن تكون أكبر من 0." : ""
                    }.Where(e => !string.IsNullOrEmpty(e)).ToList();
                    return View(new Lesson { LessonId = lessonId, CourseId = courseId, Title = title, VideoUrl = videoUrl, Duration = duration });
                }

                lesson.Title = title;
                lesson.VideoUrl = videoUrl;
                lesson.Duration = duration;
                lesson.CourseId = courseId;

                int changes = _context.SaveChanges();
                if (changes > 0)
                {
                    return RedirectToAction("Dashboard");
                }
                ViewBag.SaveError = "لم يتم حفظ التعديلات. لم يتم تسجيل أي تغييرات في قاعدة البيانات.";
            }
            catch (DbUpdateException ex)
            {
                ViewBag.SaveError = $"خطأ في قاعدة البيانات أثناء الحفظ: {ex.Message} - {ex.InnerException?.Message}";
                ViewBag.Errors = new List<string> { ex.Message };
            }
            catch (Exception ex)
            {
                ViewBag.SaveError = $"خطأ غير متوقع أثناء الحفظ: {ex.Message} - {ex.StackTrace}";
                ViewBag.Errors = new List<string> { ex.Message };
            }

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return View(new Lesson { LessonId = lessonId, CourseId = courseId, Title = title, VideoUrl = videoUrl, Duration = duration });
        }

        public IActionResult DeleteLesson(int id)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            var lesson = _context.Lessons.Find(id);
            if (lesson != null)
            {
                try
                {
                    _context.Lessons.Remove(lesson);
                    int changes = _context.SaveChanges();
                    if (changes == 0)
                    {
                        ViewBag.SaveError = "لم يتم حذف الدرس.";
                        return View("Dashboard");
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.SaveError = $"خطأ أثناء الحذف: {ex.Message}";
                    return View("Dashboard");
                }
            }

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return RedirectToAction("Dashboard");
        }
        [HttpGet]
        public IActionResult ManageQuestions(int? lessonId)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");

            ViewBag.LessonId = lessonId;
            var lessons = _context.Lessons.ToList();
            ViewBag.Lessons = lessons;

            string lessonTitle = null;
            if (lessonId.HasValue)
            {
                var lesson = _context.Lessons.Find(lessonId.Value);
                lessonTitle = lesson?.Title ?? "غير محدد";
            }
            ViewBag.LessonTitle = lessonTitle;

            if (lessonId.HasValue)
            {
                var questions = _context.Questions
                    .Where(q => q.LessonId == lessonId.Value)
                    .Include(q => q.Lesson)
                    .ThenInclude(l => l.Course)
                    .ToList();

                ViewBag.MultipleChoiceQuestions = questions.Where(q => q.QuestionType == "MultipleChoice").ToList();
                ViewBag.TrueFalseQuestions = questions.Where(q => q.QuestionType == "TrueFalse").ToList();
                ViewBag.TextQuestions = questions.Where(q => q.QuestionType == "Text").ToList();

                _logger.LogInformation("ManageQuestions called with lessonId: {LessonId}", lessonId);
                return View(questions);
            }

            _logger.LogInformation("ManageQuestions called with lessonId: (null)");
            return View(new List<Question>());
        }
        [HttpGet]
        [Route("Admin/AddMultipleChoiceQuestion/{lessonId}")]
        public IActionResult AddMultipleChoiceQuestion(int lessonId)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");
            var lesson = _context.Lessons.Find(lessonId);
            if (lesson == null) return NotFound("الدرس غير موجود.");
            ViewBag.LessonId = lessonId;
            ViewBag.LessonTitle = lesson.Title;
            return View(new AddMultipleChoiceQuestionViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/AddMultipleChoiceQuestion/{lessonId}")]
        public IActionResult AddMultipleChoiceQuestion(int lessonId, AddMultipleChoiceQuestionViewModel model)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");
            var lesson = _context.Lessons.Find(lessonId);
            if (lesson == null) return NotFound("الدرس غير موجود.");

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.QuestionText) || string.IsNullOrWhiteSpace(model.CorrectAnswer) ||
                string.IsNullOrWhiteSpace(model.OptionA) || string.IsNullOrWhiteSpace(model.OptionB))
            {
                ModelState.AddModelError("", "جميع الحقول المطلوبة (نص السؤال، الخيارات الأولى والثانية، الإجابة الصحيحة) يجب أن تكون مملوءة.");
                ViewBag.LessonId = lessonId;
                ViewBag.LessonTitle = lesson.Title;
                return View(model);
            }

            try
            {
                var question = new Question
                {
                    LessonId = lessonId,
                    QuestionText = model.QuestionText,
                    QuestionType = "MultipleChoice",
                    OptionA = model.OptionA,
                    OptionB = model.OptionB,
                    OptionC = model.OptionC,
                    OptionD = model.OptionD,
                    CorrectAnswer = model.CorrectAnswer
                };
                ConfigureQuestionOptions(question);
                _context.Questions.Add(question);
                _context.SaveChanges();
                _logger.LogInformation("Added MultipleChoice question with ID {QuestionId}", question.QuestionId);
                return RedirectToAction("ManageQuestions", new { lessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding MultipleChoice question for LessonId {LessonId}", lessonId);
                ModelState.AddModelError("", $"خطأ أثناء الإضافة: {ex.Message}");
                ViewBag.LessonId = lessonId;
                ViewBag.LessonTitle = lesson.Title;
                return View(model);
            }
        }

        [HttpGet]
        [Route("Admin/AddTrueFalseQuestion/{lessonId}")]
        public IActionResult AddTrueFalseQuestion(int lessonId)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");
            var lesson = _context.Lessons.Find(lessonId);
            if (lesson == null) return NotFound("الدرس غير موجود.");
            ViewBag.LessonId = lessonId;
            ViewBag.LessonTitle = lesson.Title;
            return View(new AddTrueFalseQuestionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/AddTrueFalseQuestion/{lessonId}")]
        public IActionResult AddTrueFalseQuestion(int lessonId, AddTrueFalseQuestionViewModel model)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");
            var lesson = _context.Lessons.Find(lessonId);
            if (lesson == null) return NotFound("الدرس غير موجود.");

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.QuestionText) ||
                string.IsNullOrWhiteSpace(model.CorrectAnswer) || !new[] { "True", "False" }.Contains(model.CorrectAnswer))
            {
                ModelState.AddModelError("", "نص السؤال والإجابة الصحيحة (True/False) يجب أن تكون مملوءة.");
                ViewBag.LessonId = lessonId;
                ViewBag.LessonTitle = lesson.Title;
                return View(model);
            }

            try
            {
                var question = new Question
                {
                    LessonId = lessonId,
                    QuestionText = model.QuestionText,
                    QuestionType = "TrueFalse",
                    CorrectAnswer = model.CorrectAnswer
                };
                ConfigureQuestionOptions(question);
                _context.Questions.Add(question);
                _context.SaveChanges();
                _logger.LogInformation("Added TrueFalse question with ID {QuestionId}", question.QuestionId);
                return RedirectToAction("ManageQuestions", new { lessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding TrueFalse question for LessonId {LessonId}", lessonId);
                ModelState.AddModelError("", $"خطأ أثناء الإضافة: {ex.Message}");
                ViewBag.LessonId = lessonId;
                ViewBag.LessonTitle = lesson.Title;
                return View(model);
            }
        }
        [HttpGet]
        [Route("Admin/AddTextQuestion/{lessonId}")]
        public IActionResult AddTextQuestion(int lessonId)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");
            var lesson = _context.Lessons.Find(lessonId);
            if (lesson == null) return NotFound("الدرس غير موجود.");
            ViewBag.LessonId = lessonId;
            ViewBag.LessonTitle = lesson.Title;
            return View(new AddTextQuestionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/AddTextQuestion/{lessonId}")]
        public IActionResult AddTextQuestion(int lessonId, AddTextQuestionViewModel model)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");
            var lesson = _context.Lessons.Find(lessonId);
            if (lesson == null) return NotFound("الدرس غير موجود.");

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.QuestionText) || string.IsNullOrWhiteSpace(model.CorrectAnswer))
            {
                ModelState.AddModelError("", "نص السؤال والإجابة الصحيحة يجب أن تكون مملوءة.");
                ViewBag.LessonId = lessonId;
                ViewBag.LessonTitle = lesson.Title;
                return View(model);
            }

            try
            {
                var question = new Question
                {
                    LessonId = lessonId,
                    QuestionText = model.QuestionText,
                    QuestionType = "Text",
                    CorrectAnswer = model.CorrectAnswer
                };
                ConfigureQuestionOptions(question);
                _context.Questions.Add(question);
                _context.SaveChanges();
                _logger.LogInformation("Added Text question with ID {QuestionId}", question.QuestionId);
                return RedirectToAction("ManageQuestions", new { lessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding Text question for LessonId {LessonId}", lessonId);
                ModelState.AddModelError("", $"خطأ أثناء الإضافة: {ex.Message}");
                ViewBag.LessonId = lessonId;
                ViewBag.LessonTitle = lesson.Title;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult EditTextQuestion(int id)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");

            var question = _context.Questions
                .Include(q => q.Lesson)
                .FirstOrDefault(q => q.QuestionId == id && q.QuestionType == "Text");
            if (question == null) return NotFound("السؤال غير موجود أو ليس من نوع إجابة نصية.");

            ViewBag.LessonId = question.LessonId;
            ViewBag.LessonTitle = question.Lesson.Title;
            var model = new EditTextQuestionViewModel
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                CorrectAnswer = question.CorrectAnswer
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTextQuestion(int id, EditTextQuestionViewModel model)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");
            if (id != model.QuestionId) return BadRequest("معرف السؤال غير متطابق.");

            var question = _context.Questions
                .Include(q => q.Lesson)
                .FirstOrDefault(q => q.QuestionId == id && q.QuestionType == "Text");
            if (question == null) return NotFound("السؤال غير موجود أو ليس من نوع إجابة نصية.");

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.QuestionText) || string.IsNullOrWhiteSpace(model.CorrectAnswer))
            {
                ModelState.AddModelError("", "نص السؤال والإجابة الصحيحة يجب أن تكون مملوءة.");
                ViewBag.LessonId = question.LessonId;
                ViewBag.LessonTitle = question.Lesson.Title;
                return View(model);
            }

            try
            {
                question.QuestionText = model.QuestionText;
                question.CorrectAnswer = model.CorrectAnswer;
                ConfigureQuestionOptions(question);
                _context.Update(question);
                _context.SaveChanges();
                _logger.LogInformation("Updated Text question with ID {QuestionId}", question.QuestionId);
                return RedirectToAction("ManageQuestions", new { lessonId = question.LessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing Text question ID {QuestionId}", id);
                ModelState.AddModelError("", $"خطأ أثناء التعديل: {ex.Message}");
                ViewBag.LessonId = question.LessonId;
                ViewBag.LessonTitle = question.Lesson.Title;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult EditTrueFalseQuestion(int id)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");
            var question = _context.Questions
                .Include(q => q.Lesson)
                .FirstOrDefault(q => q.QuestionId == id && q.QuestionType == "TrueFalse");
            if (question == null) return NotFound("السؤال غير موجود أو ليس من نوع صح/خطأ.");

            ViewBag.LessonId = question.LessonId;
            ViewBag.LessonTitle = question.Lesson.Title;
            var model = new EditTrueFalseQuestionViewModel
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                CorrectAnswer = question.CorrectAnswer
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTrueFalseQuestion(int id, EditTrueFalseQuestionViewModel model)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");
            if (id != model.QuestionId) return BadRequest("معرف السؤال غير متطابق.");

            var question = _context.Questions
                .Include(q => q.Lesson)
                .FirstOrDefault(q => q.QuestionId == id && q.QuestionType == "TrueFalse");
            if (question == null) return NotFound("السؤال غير موجود أو ليس من نوع صح/خطأ.");

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.QuestionText) ||
                string.IsNullOrWhiteSpace(model.CorrectAnswer) || !new[] { "True", "False" }.Contains(model.CorrectAnswer))
            {
                ModelState.AddModelError("", "نص السؤال والإجابة الصحيحة (True/False) يجب أن تكون مملوءة.");
                ViewBag.LessonId = question.LessonId;
                ViewBag.LessonTitle = question.Lesson.Title;
                return View(model);
            }

            try
            {
                question.QuestionText = model.QuestionText;
                question.CorrectAnswer = model.CorrectAnswer;
                ConfigureQuestionOptions(question);
                _context.Update(question);
                _context.SaveChanges();
                _logger.LogInformation("Updated TrueFalse question with ID {QuestionId}", question.QuestionId);
                return RedirectToAction("ManageQuestions", new { lessonId = question.LessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing TrueFalse question ID {QuestionId}", id);
                ModelState.AddModelError("", $"خطأ أثناء التعديل: {ex.Message}");
                ViewBag.LessonId = question.LessonId;
                ViewBag.LessonTitle = question.Lesson.Title;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult EditMultipleChoiceQuestion(int id)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");

            var question = _context.Questions
                .Include(q => q.Lesson)
                .FirstOrDefault(q => q.QuestionId == id && q.QuestionType == "MultipleChoice");
            if (question == null) return NotFound("السؤال غير موجود أو ليس من نوع اختيار متعدد.");

            ViewBag.LessonId = question.LessonId;
            ViewBag.LessonTitle = question.Lesson.Title;
            var model = new EditMultipleChoiceQuestionViewModel
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                OptionA = question.OptionA,
                OptionB = question.OptionB,
                OptionC = question.OptionC,
                OptionD = question.OptionD,
                CorrectAnswer = question.CorrectAnswer
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMultipleChoiceQuestion(int id, EditMultipleChoiceQuestionViewModel model)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");
            if (id != model.QuestionId) return BadRequest("معرف السؤال غير متطابق.");

            var question = _context.Questions
                .Include(q => q.Lesson)
                .FirstOrDefault(q => q.QuestionId == id && q.QuestionType == "MultipleChoice");
            if (question == null) return NotFound("السؤال غير موجود أو ليس من نوع اختيار متعدد.");

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.QuestionText) || string.IsNullOrWhiteSpace(model.CorrectAnswer) ||
                string.IsNullOrWhiteSpace(model.OptionA) || string.IsNullOrWhiteSpace(model.OptionB))
            {
                ModelState.AddModelError("", "نص السؤال، الخيارات الأولى والثانية، والإجابة الصحيحة يجب أن تكون مملوءة.");
                ViewBag.LessonId = question.LessonId;
                ViewBag.LessonTitle = question.Lesson.Title;
                return View(model);
            }

            try
            {
                question.QuestionText = model.QuestionText;
                question.OptionA = model.OptionA;
                question.OptionB = model.OptionB;
                question.OptionC = model.OptionC;
                question.OptionD = model.OptionD;
                question.CorrectAnswer = model.CorrectAnswer;
                ConfigureQuestionOptions(question);
                _context.Update(question);
                _context.SaveChanges();
                _logger.LogInformation("Updated MultipleChoice question with ID {QuestionId}", question.QuestionId);
                return RedirectToAction("ManageQuestions", new { lessonId = question.LessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing MultipleChoice question ID {QuestionId}", id);
                ModelState.AddModelError("", $"خطأ أثناء التعديل: {ex.Message}");
                ViewBag.LessonId = question.LessonId;
                ViewBag.LessonTitle = question.Lesson.Title;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteQuestion(int id)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");
            var question = _context.Questions.Find(id);
            if (question == null) return NotFound("السؤال غير موجود.");

            try
            {
                var userAnswers = _context.UserAnswers.Where(ua => ua.QuestionId == id);
                _context.UserAnswers.RemoveRange(userAnswers);
                _context.Questions.Remove(question);
                _context.SaveChanges();
                _logger.LogInformation("Deleted question with ID {QuestionId}", id);
                return RedirectToAction("ManageQuestions", new { lessonId = question.LessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question ID {QuestionId}", id);
                TempData["Error"] = $"خطأ أثناء الحذف: {ex.Message}";
                return RedirectToAction("ManageQuestions", new { lessonId = question.LessonId });
            }
        }

        private bool IsAdminLoggedIn()
        {
            return HttpContext.Session.GetInt32("UserId") != null &&
                   HttpContext.Session.GetString("Role") == "Admin";
        }

        private void ConfigureQuestionOptions(Question question, Question source = null)
        {
            if (question.QuestionType == "TrueFalse")
            {
                question.OptionA = "True";
                question.OptionB = "False";
                question.OptionC = null;
                question.OptionD = null;
            }
            else if (question.QuestionType == "Text")
            {
                question.OptionA = null;
                question.OptionB = null;
                question.OptionC = null;
                question.OptionD = null;
            }
            else if (question.QuestionType == "MultipleChoice" && source != null)
            {
                question.OptionA = string.IsNullOrEmpty(source.OptionA) ? null : source.OptionA;
                question.OptionB = string.IsNullOrEmpty(source.OptionB) ? null : source.OptionB;
                question.OptionC = string.IsNullOrEmpty(source.OptionC) ? null : source.OptionC;
                question.OptionD = string.IsNullOrEmpty(source.OptionD) ? null : source.OptionD;
            }
        }// إدارة المدفوعات
        public IActionResult PaymentsReport()
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية للوصول إلى هذه الصفحة.");
            }

            try
            {
                var report = _context.Payments
                    .Include(p => p.User)
                    .Include(p => p.Course)
                    .GroupBy(p => p.UserId)
                    .Select(g => new PaymentReportViewModel
                    {
                        UserId = g.Key,
                        UserName = g.First().User.FullName,
                        TotalPayments = g.Count(),
                        TotalAmount = g.Sum(p => p.Amount),
                        Subscriptions = g.Select(p => new SubscriptionDetail
                        {
                            CourseTitle = p.Course.Title,
                            Amount = p.Amount,
                            PaymentDate = p.CreatedAt
                        }).ToList()
                    }).ToList();

                // تعيين قيمة افتراضية آمنة لـ ViewBag
                ViewBag.IsLoggedIn = true;
                ViewBag.IsAdmin = true;
                ViewBag.UnreadNotifications = 0;

                return View(report);
            }
            catch (Exception ex)
            {
                return Content($"خطأ في تحميل تقرير المدفوعات: {ex.Message}");
            }
        }

        public IActionResult EditIntroVideo(int courseId)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية للوصول إلى هذه الصفحة.");
            }

            var course = _context.Courses.Find(courseId);
            if (course == null)
            {
                return NotFound();
            }

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return View(course);
        }

        [HttpPost]
        public IActionResult EditIntroVideo(int courseId, Course course)
        {
            // التحقق من تسجيل الدخول ودور المستخدم باستخدام الجلسات
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized("ليس لديك الصلاحيات الكافية لإجراء هذا الإجراء.");
            }

            if (courseId != course.CourseId)
            {
                return BadRequest();
            }

            var existingCourse = _context.Courses.Find(courseId);
            if (existingCourse == null)
            {
                return NotFound();
            }

            existingCourse.IntroVideoUrl = course.IntroVideoUrl;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "تم تحديث رابط الفيديو التعريفي بنجاح.";

            // تعيين قيمة افتراضية آمنة لـ ViewBag
            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = true;
            ViewBag.UnreadNotifications = 0;

            return RedirectToAction("Dashboard");
        }
        [HttpGet]
        public IActionResult ManageCertificateConditions()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login", "Account");

            try
            {
                var viewModel = _context.Courses
                    .Select(c => new CertificateConditionViewModel
                    {
                        CourseId = c.CourseId,
                        CourseTitle = c.Title ?? "بدون عنوان",
                        Conditions = _context.CertificateConditions
                            .Where(cc => cc.CourseId == c.CourseId)
                            .ToList()
                    })
                    .ToList();

                _logger.LogInformation("Loaded certificate conditions management page.");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ManageCertificateConditions.");
                TempData["Error"] = "حدث خطأ أثناء تحميل صفحة إدارة شروط الشهادات.";
                return View(new List<CertificateConditionViewModel>());
            }
        }

        // إضافة شرط جديد (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCertificateCondition(CertificateCondition condition)
        {
            if (!IsAdminLoggedIn()) return Json(new { success = false, message = "غير مصرح لك." });

            _logger.LogInformation("AddCertificateCondition called with: ConditionId={ConditionId}, CourseId={CourseId}, ConditionType={ConditionType}, Value={Value}, Description={Description}",
                condition.ConditionId, condition.CourseId, condition.ConditionType, condition.Value, condition.Description);

            // التحقق اليدوي فقط
            if (condition.CourseId <= 0)
            {
                _logger.LogWarning("CourseId is invalid: {CourseId}", condition.CourseId);
                return Json(new { success = false, message = "يرجى اختيار دورة صالحة." });
            }
            if (string.IsNullOrWhiteSpace(condition.ConditionType))
            {
                _logger.LogWarning("ConditionType is empty or null.");
                return Json(new { success = false, message = "نوع الشرط مطلوب." });
            }
            if (condition.Value < 0)
            {
                _logger.LogWarning("Value is negative: {Value}", condition.Value);
                return Json(new { success = false, message = "القيمة يجب أن تكون غير سالبة." });
            }

            try
            {
                if (!_context.Courses.Any(c => c.CourseId == condition.CourseId))
                {
                    _logger.LogWarning("Course with ID {CourseId} not found.", condition.CourseId);
                    return Json(new { success = false, message = "الدورة المحددة غير موجودة." });
                }

                _context.CertificateConditions.Add(condition);
                _context.SaveChanges();
                _logger.LogInformation("Added CertificateCondition with ID {ConditionId} for CourseId {CourseId}", condition.ConditionId, condition.CourseId);

                return Json(new { success = true, conditionId = condition.ConditionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding CertificateCondition for CourseId {CourseId}", condition.CourseId);
                return Json(new { success = false, message = $"خطأ أثناء الإضافة: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCertificateCondition(CertificateCondition condition)
        {
            if (!IsAdminLoggedIn()) return Json(new { success = false, message = "غير مصرح لك." });

            _logger.LogInformation("UpdateCertificateCondition called with: ConditionId={ConditionId}, CourseId={CourseId}, ConditionType={ConditionType}, Value={Value}, Description={Description}",
                condition.ConditionId, condition.CourseId, condition.ConditionType, condition.Value, condition.Description);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "خطأ في البيانات: " + string.Join(", ", errors) });
            }

            if (condition.CourseId <= 0)
            {
                return Json(new { success = false, message = "يرجى اختيار دورة صالحة." });
            }
            if (string.IsNullOrWhiteSpace(condition.ConditionType))
            {
                return Json(new { success = false, message = "نوع الشرط مطلوب." });
            }
            if (condition.Value < 0)
            {
                return Json(new { success = false, message = "القيمة يجب أن تكون غير سالبة." });
            }

            try
            {
                var existingCondition = _context.CertificateConditions.Find(condition.ConditionId);
                if (existingCondition == null)
                {
                    return Json(new { success = false, message = "الشرط غير موجود." });
                }

                if (!_context.Courses.Any(c => c.CourseId == condition.CourseId))
                {
                    return Json(new { success = false, message = "الدورة المحددة غير موجودة." });
                }

                existingCondition.ConditionType = condition.ConditionType;
                existingCondition.Value = condition.Value;
                existingCondition.Description = condition.Description;
                existingCondition.CourseId = condition.CourseId;
                _context.Update(existingCondition);
                _context.SaveChanges();
                _logger.LogInformation("Updated CertificateCondition with ID {ConditionId}", condition.ConditionId);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating CertificateCondition ID {ConditionId}", condition.ConditionId);
                return Json(new { success = false, message = $"خطأ أثناء التعديل: {ex.Message}" });
            }
        }

        // حذف شرط (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCertificateCondition(int conditionId)
        {
            if (!IsAdminLoggedIn()) return Json(new { success = false, message = "غير مصرح لك." });

            try
            {
                var condition = _context.CertificateConditions.Find(conditionId);
                if (condition == null)
                {
                    return Json(new { success = false, message = "الشرط غير موجود." });
                }

                _context.CertificateConditions.Remove(condition);
                _context.SaveChanges();
                _logger.LogInformation("Deleted CertificateCondition with ID {ConditionId}", conditionId);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting CertificateCondition ID {ConditionId}", conditionId);
                return Json(new { success = false, message = $"خطأ أثناء الحذف: {ex.Message}" });
            }
        }
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
                .Where(q => q.UserId == studentId && q.Lesson.CourseId == courseId) // تصحيح StudentId إلى UserId
                .ToList();

            bool isEligible = true;
            foreach (var condition in course.CertificateConditions)
            {
                if (condition.ConditionType == "CompletionPercentage")
                {
                    var completedLessons = progress.Count(p => p.IsCompleted);
                    var totalLessons = course.Lessons.Count;
                    var percentage = (completedLessons / (float)totalLessons) * 100;
                    if (percentage < (float)condition.Value) // تحويل condition.Value إلى float للمقارنة
                        isEligible = false;
                }
                else if (condition.ConditionType == "QuizScore")
                {
                    var avgScore = quizResults.Any() ? quizResults.Average(q => q.Score) : 0; // Score هو float
                    if ((decimal)avgScore < condition.Value) // تحويل avgScore إلى decimal للمقارنة
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

                // إضافة إشعار
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

        private string GenerateCertificatePdf(int studentId, int courseId)
        {
            var student = _context.Users.Find(studentId);
            var course = _context.Courses.Find(courseId);
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/certificates", $"certificate_{studentId}_{courseId}.pdf");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(20).FontFamily("Tajawal"));

                    page.Header()
                        .Text("شهادة إتمام دورة")
                        .SemiBold().FontSize(36).FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
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
    }

    // نماذج مساعدة للعرض
    public class AdminDashboardViewModel
        {
            public List<User> Users { get; set; } = new List<User>(); // تهيئة افتراضية للتعامل مع القوائم الفارغة
            public List<Course> Courses { get; set; } = new List<Course>();
            public List<Payment> Payments { get; set; } = new List<Payment>();
        }

        public class PaymentReportViewModel
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public int TotalPayments { get; set; }
            public decimal TotalAmount { get; set; }
            public List<SubscriptionDetail> Subscriptions { get; set; }
        }

        public class SubscriptionDetail
        {
            public string CourseTitle { get; set; }
            public decimal Amount { get; set; }
            public DateTime PaymentDate { get; set; }
        }

        public class TeacherDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
}
