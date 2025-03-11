using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeaderSurvey.Models
{
    public class SurveyResponse
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public int LeaderId { get; set; }
        
        private DateTime _completionDate = DateTime.UtcNow;

        [Column(TypeName = "timestamp with time zone")]
        public DateTime CompletionDate 
        { 
            get => _completionDate;
            set => _completionDate = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
        
        public Survey? Survey { get; set; }
        public Leader? Leader { get; set; }
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();

        [NotMapped]
        public DateTime LocalCompletionDate 
        {
            get => CompletionDate.ToLocalTime();
            set => CompletionDate = value.ToUniversalTime();
        }
    }
}   