using System.ComponentModel.DataAnnotations;

namespace LeaderSurvey.Models
{
    public class SurveyResponse
    {
        public int Id { get; set; }
        
        public int SurveyId { get; set; }
        public Survey? Survey { get; set; }
        
        public int LeaderId { get; set; }
        public Leader? Leader { get; set; }
        
        public DateTime CompletionDate { get; set; } = DateTime.UtcNow;
        
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}   