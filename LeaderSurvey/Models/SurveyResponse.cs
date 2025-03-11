using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeaderSurvey.Models
{
    public class SurveyResponse
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public int LeaderId { get; set; }
        
        private DateTimeOffset _completionDate = DateTimeOffset.UtcNow;

        public DateTimeOffset CompletionDate 
        { 
            get => _completionDate;
            set => _completionDate = value;
        }
        
        public Survey? Survey { get; set; }
        public Leader? Leader { get; set; }
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();

        [NotMapped]
        public DateTimeOffset LocalCompletionDate 
        {
            get => CompletionDate.ToLocalTime();
            set => CompletionDate = value.ToUniversalTime();
        }
    }
}   