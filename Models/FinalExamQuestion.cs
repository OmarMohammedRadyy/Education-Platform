namespace EducationPlatformN.Models
{
    public class FinalExamQuestion
    {
        public int FinalExamQuestionId { get; set; }
        public int FinalExamId { get; set; }
        public string QuestionText { get; set; }
        public string CorrectAnswer { get; set; }
        public string QuestionType { get; set; } // "MultipleChoice", "TrueFalse", "Text"
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public virtual FinalExam FinalExam { get; set; }
    }
}
