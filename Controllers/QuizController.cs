using EducationPlatformN.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using EducationPlatformN.Services;

namespace EducationPlatformN.Controllers
{
    public class QuizController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICertificateService _certificateService;

        public QuizController(AppDbContext context, IWebHostEnvironment env, ICertificateService certificateService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _certificateService = certificateService ?? throw new ArgumentNullException(nameof(certificateService));
        }

        [HttpGet]
        public IActionResult TakeQuiz(int lessonId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var questions = _context.Questions
                .Where(q => q.LessonId == lessonId)
                .ToList();

            if (!questions.Any())
            {
                return NotFound("لا توجد أسئلة لهذا الدرس.");
            }

            ViewBag.LessonId = lessonId;
            return View(questions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuiz(int lessonId, Dictionary<int, string> answers)
        {
            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var questions = _context.Questions.Where(q => q.LessonId == lessonId).ToList();
            var lesson = await _context.Lessons.FindAsync(lessonId);
            var courseId = lesson?.CourseId ?? 0;

            foreach (var answer in answers)
            {
                var question = questions.FirstOrDefault(q => q.QuestionId == answer.Key);
                if (question != null)
                {
                    _context.UserAnswers.Add(new UserAnswer
                    {
                        UserId = userId,
                        QuestionId = answer.Key,
                        SelectedAnswer = answer.Value,
                        AnsweredAt = DateTime.Now
                    });
                }
            }
            await _context.SaveChangesAsync();

            var userAnswers = _context.UserAnswers
                .Include(ua => ua.Question)
                .Where(ua => ua.UserId == userId && questions.Select(q => q.QuestionId).Contains(ua.QuestionId))
                .ToList();

            int correctAnswers = userAnswers.Count(ua => ua.Question.QuestionType == "Text"
                ? ua.SelectedAnswer.Trim().ToLower() == ua.Question.CorrectAnswer.Trim().ToLower()
                : ua.SelectedAnswer == ua.Question.CorrectAnswer);

            var score = (double)correctAnswers / questions.Count * 100;
            var quizResult = new QuizResult
            {
                UserId = userId,
                LessonId = lessonId,
                Score = score,
                CompletionDate = DateTime.Now,
                AnswersJson = JsonConvert.SerializeObject(answers)
            };
            _context.QuizResults.Add(quizResult);
            await _context.SaveChangesAsync();

            var lessonCount = _context.Lessons.Count(l => l.CourseId == courseId);
            var completedLessons = _context.QuizResults.Count(qr => qr.UserId == userId && qr.Lesson.CourseId == courseId);
            var progress = (double)completedLessons / lessonCount * 100;

            TempData["SuccessMessage"] = $"تهانينا! أكملت الاختبار بنتيجة {score:F2}%. تقدمك: {progress:F2}%";
            return RedirectToAction("QuizResults", new { lessonId });
        }

        [HttpGet]
        public IActionResult QuizResults(int lessonId)
        {
            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var latestResult = _context.QuizResults
                .Include(qr => qr.Lesson)
                .ThenInclude(l => l.Questions)
                .Where(qr => qr.UserId == userId && qr.LessonId == lessonId)
                .OrderByDescending(qr => qr.CompletionDate)
                .FirstOrDefault();

            if (latestResult == null) return NotFound();

            var viewModel = new QuizResultViewModel
            {
                LessonTitle = latestResult.Lesson.Title,
                Score = latestResult.Score,
                CompletionDate = latestResult.CompletionDate,
                Answers = _context.UserAnswers
                    .Include(ua => ua.Question)
                    .Where(ua => ua.UserId == userId && ua.Question.LessonId == lessonId)
                    .OrderByDescending(ua => ua.AnsweredAt)
                    .Take(latestResult.Lesson.Questions.Count)
                    .Select(ua => new AnswerDetail
                    {
                        LessonId = ua.Question.LessonId,
                        QuestionId = ua.QuestionId.ToString(),
                        QuestionText = ua.Question.QuestionText,
                        SelectedAnswer = ua.SelectedAnswer,
                        CorrectAnswer = ua.Question.CorrectAnswer,
                        IsCorrect = ua.Question.QuestionType == "Text"
                            ? ua.SelectedAnswer.Trim().ToLower() == ua.Question.CorrectAnswer.Trim().ToLower()
                            : ua.SelectedAnswer == ua.Question.CorrectAnswer
                    }).ToList()
            };

            ViewBag.AttemptHistoryUrl = Url.Action("QuizAttemptHistory", new { lessonId });
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult QuizAttemptHistory(int lessonId)
        {
            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var attempts = _context.QuizResults
                .Where(qr => qr.UserId == userId && qr.LessonId == lessonId)
                .OrderByDescending(qr => qr.CompletionDate)
                .Select(qr => new { qr.Score, qr.CompletionDate })
                .ToList();

            return View(attempts);
        }

        [HttpGet]
        public async Task<IActionResult> STDashboard(int courseId, string filterLessonTitle = null, DateTime? filterStartDate = null, DateTime? filterEndDate = null, double? filterMinScore = null)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound("الدورة غير موجودة.");

            var lessons = _context.Lessons.Where(l => l.CourseId == courseId).ToList();
            var quizResultsQuery = _context.QuizResults
                .Where(qr => qr.UserId == userId && qr.Lesson.CourseId == courseId);

            if (!string.IsNullOrEmpty(filterLessonTitle))
            {
                quizResultsQuery = quizResultsQuery.Where(qr => qr.Lesson.Title.Contains(filterLessonTitle));
            }
            if (filterStartDate.HasValue)
            {
                quizResultsQuery = quizResultsQuery.Where(qr => qr.CompletionDate >= filterStartDate.Value);
            }
            if (filterEndDate.HasValue)
            {
                quizResultsQuery = quizResultsQuery.Where(qr => qr.CompletionDate <= filterEndDate.Value);
            }
            if (filterMinScore.HasValue)
            {
                quizResultsQuery = quizResultsQuery.Where(qr => qr.Score >= filterMinScore.Value);
            }

            var quizResults = quizResultsQuery.ToList();
            var certificates = await _certificateService.GetUserCertificatesAsync(userId);

            var viewModel = new StudentDashboardViewModel
            {
                CourseProgress = lessons.Any() ? (double)_context.QuizResults.Count(qr => qr.UserId == userId && qr.Lesson.CourseId == courseId) / lessons.Count * 100 : 0,
                Lessons = lessons.Select(l => new LessonProgress
                {
                    LessonId = l.LessonId,
                    LessonTitle = l.Title,
                    Score = _context.QuizResults.Where(qr => qr.LessonId == l.LessonId && qr.UserId == userId)
                        .OrderByDescending(qr => qr.CompletionDate)
                        .FirstOrDefault()?.Score ?? 0,
                    AttemptCount = _context.QuizResults.Count(qr => qr.LessonId == l.LessonId && qr.UserId == userId)
                }).ToList(),
                QuizHistory = quizResults.Select(qr => new QuizAttempt
                {
                    LessonId = qr.LessonId,
                    LessonTitle = qr.Lesson.Title,
                    Score = qr.Score,
                    CompletionDate = qr.CompletionDate
                }).OrderByDescending(q => q.CompletionDate).ToList(),
                Certificates = certificates.Select(c => new CertificateDisplayModel
                {
                    CourseTitle = c.Course.Title,
                    IssuedDate = c.IssuedDate,
                    CertificateFileName = c.CertificateUrl.Split('/').Last()
                }).ToList(),
                Notes = _context.LessonNotes.Where(n => n.Lesson.CourseId == courseId).ToList(),
                FilterLessonTitle = filterLessonTitle,
                FilterStartDate = filterStartDate,
                FilterEndDate = filterEndDate,
                FilterMinScore = filterMinScore
            };

            ViewBag.Notifications = _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Certificates()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var certificates = await _certificateService.GetUserCertificatesAsync(userId);

            var viewModel = new CertificateViewModel
            {
                Certificates = certificates.Select(c => new CertificateDisplayModel
                {
                    CourseTitle = c.Course.Title,
                    IssuedDate = c.IssuedDate,
                    CertificateFileName = Path.GetFileName(c.CertificateUrl)
                }).ToList(),
                IsLoggedIn = true,
                IsAdmin = HttpContext.Session.GetString("Role") == "Admin",
                UnreadNotifications = _context.Notifications.Count(n => n.UserId == userId && !n.IsRead)
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> FinalExam(int courseId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == userId.Value && e.CourseId == courseId);

                if (enrollment == null)
                {
                    return Unauthorized("غير مسجل في هذه الدورة.");
                }

                var progress = await _context.StudentProgress
                    .Where(sp => sp.StudentId == userId.Value && sp.Lesson.CourseId == courseId)
                    .ToListAsync();

                var totalLessons = await _context.Lessons.CountAsync(l => l.CourseId == courseId);
                var completedLessons = progress.Count(sp => sp.ProgressPercentage >= 100);

                if (completedLessons < totalLessons)
                {
                    TempData["ErrorMessage"] = "يجب إكمال جميع الدروس أولاً.";
                    return RedirectToAction("STDashboard", new { courseId });
                }

                var finalExam = await _context.FinalExams
                    .Include(fe => fe.FinalExamQuestions)
                    .FirstOrDefaultAsync(fe => fe.CourseId == courseId);

                if (finalExam == null)
                {
                    return NotFound("لا يوجد اختبار نهائي لهذه الدورة بعد.");
                }

                return View(finalExam);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FinalExam: {ex.Message}");
                return StatusCode(500, "حدث خطأ داخلي. يرجى المحاولة لاحقًا.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFinalExam(int finalExamId, Dictionary<int, string> answers)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized("المستخدم غير مصدق.");
            }

            var finalExam = await _context.FinalExams
                .Include(fe => fe.FinalExamQuestions)
                .FirstOrDefaultAsync(fe => fe.FinalExamId == finalExamId);

            if (finalExam == null)
            {
                return NotFound("الاختبار غير موجود.");
            }

            double score = 0;
            foreach (var question in finalExam.FinalExamQuestions)
            {
                if (answers.TryGetValue(question.FinalExamQuestionId, out var studentAnswer) &&
                    studentAnswer == question.CorrectAnswer)
                {
                    score += 1;
                }
            }
            score = (score / finalExam.FinalExamQuestions.Count) * 100;

            var result = new FinalExamResult
            {
                UserId = userId.Value,
                FinalExamId = finalExamId,
                Score = score,
                CompletionDate = DateTime.Now,
                AnswersJson = JsonConvert.SerializeObject(answers)
            };

            _context.FinalExamResults.Add(result);
            await _context.SaveChangesAsync();

            try
            {
                if (await _certificateService.IsEligibleForCertificateAsync(userId.Value, finalExam.CourseId))
                {
                    var certificate = await _certificateService.IssueCertificateAsync(userId.Value, finalExam.CourseId);
                    TempData["SuccessMessage"] = $"تهانينا! لقد حصلت على الشهادة. يمكنك تنزيلها من صفحة <a href='{Url.Action("Certificates")}'>الشهادات</a>.";
                }
                else
                {
                    TempData["InfoMessage"] = "لم تتحقق جميع الشروط بعد. حاول مجددًا بعد إكمال المتطلبات.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"خطأ أثناء إصدار الشهادة: {ex.Message}";
            }

            return RedirectToAction("Result", new { resultId = result.FinalExamResultId });
        }

        [HttpGet]
        public async Task<IActionResult> Result(int resultId)
        {
            var result = await _context.FinalExamResults
                .Include(r => r.FinalExam)
                .ThenInclude(fe => fe.Course)
                .FirstOrDefaultAsync(r => r.FinalExamResultId == resultId && r.UserId == int.Parse(User.FindFirst("UserId").Value));

            if (result == null)
            {
                return NotFound("النتيجة غير موجودة.");
            }

            return View(result);
        }


        public class QuizResultViewModel
        {
            public string LessonTitle { get; set; }
            public double Score { get; set; }
            public DateTime CompletionDate { get; set; }
            public List<AnswerDetail> Answers { get; set; }
        }
        public class AnswerDetail
        {
            public int LessonId { get; set; }
            public string QuestionId { get; set; }
            public string QuestionText { get; set; }
            public string SelectedAnswer { get; set; }
            public string CorrectAnswer { get; set; }
            public bool IsCorrect { get; set; }
        }
        public class CertificateViewModel
        {
            public List<CertificateDisplayModel> Certificates { get; set; }
            public bool IsLoggedIn { get; set; }
            public bool IsAdmin { get; set; }
            public int UnreadNotifications { get; set; }
        }
        private string GenerateCertificatePdf(int userId, int courseId)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var user = _context.Users.Find(userId);
            var course = _context.Courses.Find(courseId);
            var fileName = $"certificate_{userId}_{courseId}_{DateTime.Now.Ticks}.pdf";
            var path = Path.Combine(_env.WebRootPath, "certificates", fileName);

            if (!Directory.Exists(Path.Combine(_env.WebRootPath, "certificates")))
            {
                Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "certificates"));
            }

            var document = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("منصة عمر التعليمية")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(2, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Item().Text("شهادة إتمام دورة")
                                .FontSize(20).Bold().FontColor(Colors.Black)
                                .AlignCenter();

                            x.Item().PaddingTop(1, Unit.Centimetre)
                                .Text($"تم منح هذه الشهادة لـ: {user.FullName}")
                                .FontSize(16).FontColor(Colors.Grey.Darken2)
                                .AlignCenter();

                            x.Item().PaddingTop(1, Unit.Centimetre)
                                .Text($"لإكمال دورة: {course.Title}")
                                .FontSize(16).FontColor(Colors.Grey.Darken2)
                                .AlignCenter();

                            x.Item().PaddingTop(1, Unit.Centimetre)
                                .Text($"بتاريخ: {DateTime.Now:dd/MM/yyyy}")
                                .FontSize(14).FontColor(Colors.Black)
                                .AlignCenter();

                            x.Item().PaddingTop(2, Unit.Centimetre)
                                .Text("توقيع المشرف: ____________________")
                                .FontSize(12).AlignCenter();
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("جميع الحقوق محفوظة © 2025 منصة عمر")
                        .FontSize(10).FontColor(Colors.Grey.Lighten1);
                });
            });

            document.GeneratePdf(path);
            return $"/certificates/{fileName}";
        }

        [HttpGet]
        public IActionResult DownloadCertificate(string fileName)
        {
            var filePath = Path.Combine(_env.WebRootPath, "certificates", fileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"الملف {fileName} غير موجود.");
            }

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(fileStream, "application/pdf", fileName);
        }
    }
}