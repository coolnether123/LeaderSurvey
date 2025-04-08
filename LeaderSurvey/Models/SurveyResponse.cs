using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeaderSurvey.Models
{
    public class SurveyResponse
    {
        public int Id { get; set; }

        public int SurveyId { get; set; }
        public virtual Survey? Survey { get; set; }

        public int LeaderId { get; set; }
        public virtual Leader? Leader { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime CompletionDate { get; set; }

        public string? AdditionalNotes { get; set; }

        public virtual ICollection<Answer> Answers { get; set; } = [];
    }
}