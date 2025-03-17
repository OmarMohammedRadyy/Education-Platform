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
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<FinalExam> FinalExams { get; set; }
        public DbSet<FinalExamResult> FinalExamResults { get; set; }
        public DbSet<FinalExamQuestion> FinalExamQuestions { get; set; }
        public DbSet<LessonNote> LessonNotes { get; set; }
        public DbSet<UserAnswer> UserAnswers { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<QuizResult> QuizResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.HasMany(u => u.Courses).WithOne(c => c.Teacher).HasForeignKey(c => c.TeacherId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(u => u.Payments).WithOne(p => p.User).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(u => u.UserAnswers).WithOne(ua => ua.User).HasForeignKey(ua => ua.UserId).OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(u => u.QuizResults).WithOne(qr => qr.User).HasForeignKey(qr => qr.UserId).OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(u => u.StudentProgress).WithOne(sp => sp.Student).HasForeignKey(sp => sp.StudentId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(u => u.Certificates).WithOne(c => c.Student).HasForeignKey(c => c.StudentId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(u => u.Notifications).WithOne(n => n.User).HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(u => u.Enrollments).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(u => u.FinalExamResults).WithOne(fer => fer.User).HasForeignKey(fer => fer.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(c => c.CourseId);
                entity.Property(c => c.Price).HasColumnType("decimal(10,2)");
                entity.Property(c => c.IntroVideoUrl).IsRequired(false);
                entity.HasMany(c => c.Lessons).WithOne(l => l.Course).HasForeignKey(l => l.CourseId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(c => c.Payments).WithOne(p => p.Course).HasForeignKey(p => p.CourseId).OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(c => c.Certificates).WithOne(c => c.Course).HasForeignKey(c => c.CourseId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(c => c.CertificateConditions).WithOne(cc => cc.Course).HasForeignKey(cc => cc.CourseId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(c => c.Enrollments).WithOne(e => e.Course).HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(c => c.FinalExams).WithOne(fe => fe.Course).HasForeignKey(fe => fe.CourseId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.HasKey(l => l.LessonId);
                entity.HasMany(l => l.Questions).WithOne(q => q.Lesson).HasForeignKey(q => q.LessonId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(l => l.QuizResults).WithOne(qr => qr.Lesson).HasForeignKey(qr => qr.LessonId).OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(l => l.StudentProgress).WithOne(sp => sp.Lesson).HasForeignKey(sp => sp.LessonId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(l => l.LessonNotes).WithOne(ln => ln.Lesson).HasForeignKey(ln => ln.LessonId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.PaymentId);
                entity.Property(p => p.Amount).HasColumnType("decimal(10,2)");
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(q => q.QuestionId);
                entity.Property(q => q.OptionA).IsRequired(false);
                entity.Property(q => q.OptionB).IsRequired(false);
                entity.Property(q => q.OptionC).IsRequired(false);
                entity.Property(q => q.OptionD).IsRequired(false);
                entity.HasMany(q => q.UserAnswers).WithOne(ua => ua.Question).HasForeignKey(ua => ua.QuestionId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserAnswer>(entity =>
            {
                entity.HasKey(ua => ua.UserAnswerId);
            });

            modelBuilder.Entity<QuizResult>(entity =>
            {
                entity.HasKey(qr => qr.QuizResultId);
                entity.Property(qr => qr.AnswersJson).HasColumnType("nvarchar(max)").IsRequired();
                entity.Property(qr => qr.Score).HasColumnType("float");
            });

            modelBuilder.Entity<Certificate>(entity =>
            {
                entity.HasKey(c => c.CertificateId);
            });

            modelBuilder.Entity<CertificateCondition>(entity =>
            {
                entity.HasKey(cc => cc.ConditionId);
                entity.Property(cc => cc.Value).HasPrecision(5, 2);
            });

            modelBuilder.Entity<StudentProgress>(entity =>
            {
                entity.HasKey(sp => sp.ProgressId);
                entity.Property(sp => sp.ProgressPercentage).HasColumnType("float").IsRequired();
            });

            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.EnrollmentId);
            });

            modelBuilder.Entity<FinalExam>(entity =>
            {
                entity.HasKey(fe => fe.FinalExamId);
                entity.Property(fe => fe.Title).IsRequired(false);
                entity.HasMany(fe => fe.FinalExamResults).WithOne(fer => fer.FinalExam).HasForeignKey(fer => fer.FinalExamId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(fe => fe.FinalExamQuestions).WithOne(feq => feq.FinalExam).HasForeignKey(feq => feq.FinalExamId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FinalExamResult>(entity =>
            {
                entity.HasKey(fer => fer.FinalExamResultId);
                entity.Property(fer => fer.Score).HasColumnType("float");
                entity.Property(fer => fer.AnswersJson).HasColumnType("nvarchar(max)").IsRequired(false);
            });

            modelBuilder.Entity<FinalExamQuestion>(entity =>
            {
                entity.HasKey(feq => feq.FinalExamQuestionId);
                entity.Property(feq => feq.QuestionText).IsRequired();
                entity.Property(feq => feq.CorrectAnswer).IsRequired();
                entity.Property(feq => feq.QuestionType).IsRequired();
                entity.Property(feq => feq.OptionA).IsRequired(false);
                entity.Property(feq => feq.OptionB).IsRequired(false);
                entity.Property(feq => feq.OptionC).IsRequired(false);
                entity.Property(feq => feq.OptionD).IsRequired(false);
                entity.HasOne(feq => feq.FinalExam).WithMany(fe => fe.FinalExamQuestions).HasForeignKey(feq => feq.FinalExamId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LessonNote>(entity =>
            {
                entity.HasKey(ln => ln.LessonNoteId);
                entity.HasOne(ln => ln.UploadedBy).WithMany().HasForeignKey(ln => ln.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.NotificationId);
                entity.Property(n => n.Message).HasMaxLength(500);
                entity.Property(n => n.InvoiceUrl).IsRequired(false);
            });
        }
    }
}