using System.ComponentModel.DataAnnotations;

namespace LeaderSurvey.Models
{
    public class Question
    {
        public int Id { get; set; }
        
        [Required]
        public string Text { get; set; } = string.Empty;
        
        [Required]
        public string QuestionType { get; set; } = "Text";
        
        public int QuestionOrder { get; set; }
        
        public int SurveyId { get; set; }
        public Survey? Survey { get; set; }
    }
}