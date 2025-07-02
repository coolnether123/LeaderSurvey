using System.ComponentModel.DataAnnotations;

namespace LeaderSurvey.Models
{
    public class Answer
    {
        public int Id { get; set; }
        
        public int QuestionId { get; set; }
        public virtual Question? Question { get; set; }
        
        public int SurveyResponseId { get; set; }
        public virtual SurveyResponse? SurveyResponse { get; set; }
        
        [Required]
        public string Response { get; set; } = string.Empty;
    }
}