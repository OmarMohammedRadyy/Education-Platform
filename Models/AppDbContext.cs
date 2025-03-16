using Microsoft.EntityFrameworkCore;

namespace EducationPlatformN.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<CertificateCondition> CertificateConditions { get; set; }
        public DbSet<StudentProgress> StudentProgress { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<UserAnswer> UserAnswers { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<QuizResult> QuizResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // StudentProgress
            modelBuilder.Entity<StudentProgress>()
                .HasKey(sp => sp.ProgressId);
            modelBuilder.Entity<StudentProgress>()
                .HasOne(sp => sp.Student)
                .WithMany(u => u.StudentProgress) // ربط مع قائمة في User
                .HasForeignKey(sp => sp.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<StudentProgress>()
                .HasOne(sp => sp.Lesson)
                .WithMany(l => l.StudentProgress) // ربط مع قائمة في Lesson
                .HasForeignKey(sp => sp.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Course و Teacher (اختيارية)
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Teacher)
                .WithMany(u => u.Courses)
                .HasForeignKey(c => c.TeacherId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment و User
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment و Course
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Course)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Lesson و Course
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question و Lesson
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Lesson)
                .WithMany(l => l.Questions)
                .HasForeignKey(q => q.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserAnswer و User
            modelBuilder.Entity<UserAnswer>()
                .HasOne(ua => ua.User)
                .WithMany(u => u.UserAnswers)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Question>()
    .Property(q => q.OptionA)
    .IsRequired(false);
            modelBuilder.Entity<Question>()
                .Property(q => q.OptionB)
                .IsRequired(false);
            modelBuilder.Entity<Question>()
                .Property(q => q.OptionC)
                .IsRequired(false);
            modelBuilder.Entity<Question>()
                .Property(q => q.OptionD)
                .IsRequired(false);
            // UserAnswer و Question
            modelBuilder.Entity<UserAnswer>()
                .HasOne(ua => ua.Question)
                .WithMany(q => q.UserAnswers)
                .HasForeignKey(ua => ua.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Certificate و User (Student)
            modelBuilder.Entity<Certificate>()
                .HasKey(c => c.CertificateId);
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.Student)
                .WithMany(u => u.Certificates) // ربط مع قائمة في User
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Certificate و Course
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.Course)
                .WithMany(c => c.Certificates) // ربط مع قائمة في Course
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // CertificateCondition و Course
            modelBuilder.Entity<CertificateCondition>()
                .HasKey(cc => cc.ConditionId);
            modelBuilder.Entity<CertificateCondition>()
    .HasOne(cc => cc.Course)
    .WithMany(c => c.CertificateConditions)
    .HasForeignKey(cc => cc.CourseId)
    .IsRequired(false) // جعل العلاقة اختيارية
    .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CertificateCondition>()
                .Property(cc => cc.Value)
                .HasPrecision(5, 2); // دقة 5 أرقام، 2 بعد الفاصلة (مثل 100.00)

            // Notification و User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // QuizResult و User
            modelBuilder.Entity<QuizResult>()
                .HasOne(qr => qr.User)
                .WithMany(u => u.QuizResults)
                .HasForeignKey(qr => qr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // QuizResult و Lesson
            modelBuilder.Entity<QuizResult>()
                .HasOne(qr => qr.Lesson)
                .WithMany(l => l.QuizResults)
                .HasForeignKey(qr => qr.LessonId)
                .OnDelete(DeleteBehavior.Restrict);

            // تحديد أنواع البيانات
            modelBuilder.Entity<Course>()
                .Property(c => c.Price)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Course>()
                .Property(c => c.IntroVideoUrl)
                .IsRequired(false);

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .IsRequired(true);

            modelBuilder.Entity<Notification>()
                .Property(n => n.InvoiceUrl)
                .IsRequired(false);

            modelBuilder.Entity<QuizResult>()
                .Property(qr => qr.AnswersJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired(true);

            modelBuilder.Entity<QuizResult>()
                .Property(qr => qr.Score)
                .HasColumnType("float");
        }
    }
}