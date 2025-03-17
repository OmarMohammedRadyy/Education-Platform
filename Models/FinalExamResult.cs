namespace EducationPlatformN.Models
{
    public class FinalExamResult
    {
        public int FinalExamResultId { get; set; }
        public int UserId { get; set; }
        public int FinalExamId { get; set; }
        public double Score { get; set; }
        public DateTime CompletionDate { get; set; }
        public string AnswersJson { get; set; }
        public virtual User User { get; set; }
        public virtual FinalExam FinalExam { get; set; }
    }
}
