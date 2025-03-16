namespace EducationPlatformN.Models
{
    public class UserAnswer
    {
        public int UserAnswerId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int QuestionId { get; set; }
        public Question Question { get; set; }
        public string SelectedAnswer { get; set; }
        public DateTime AnsweredAt { get; set; }
    }
}
