using EducationPlatformN.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using BCrypt.Net;

namespace EducationPlatformN.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                ViewBag.Error = "البريد الإلكتروني وكلمة المرور مطلوبين.";
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ViewBag.Error = "بيانات تسجيل الدخول غير صحيحة.";
                return View();
            }

            // تأكد من أن الجلسة مفعلة
            if (HttpContext.Session == null)
            {
                throw new InvalidOperationException("Session is not available. Ensure UseSession() is called in Program.cs.");
            }

            // تخزين بيانات المستخدم في الجلسة
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("FullName", user.FullName ?? "مستخدم");
            HttpContext.Session.SetString("Role", user.Role ?? "Student");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User model)
        {
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ViewBag.Error = "البريد الإلكتروني مستخدم بالفعل.";
                    return View(model);
                }

                model.CreatedAt = DateTime.Now;
                model.Role = model.Role ?? "Student";
                model.Language = model.Language ?? "ar";

                // توليد Salt تلقائيًا واستخدامه في HashPassword
                string salt = BCrypt.Net.BCrypt.GenerateSalt();
                model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash, salt, enhancedEntropy: false, hashType: HashType.SHA256);

                _context.Users.Add(model);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }

            ViewBag.Error = "البيانات غير صحيحة. يرجى التحقق.";
            return View(model);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}