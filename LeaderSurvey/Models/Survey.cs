﻿using System.ComponentModel.DataAnnotations;

namespace LeaderSurvey.Models
{
    public class Survey
    {
        private DateTime _createdDate = DateTime.UtcNow;
        private DateTime? _monthYear;
        private DateTime? _completedDate;

        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Area { get; set; } = string.Empty;

        public DateTime CreatedDate 
        { 
            get => _createdDate;
            set => _createdDate = value.Kind == DateTimeKind.Unspecified ? 
                DateTime.SpecifyKind(value, DateTimeKind.Utc) : 
                value.ToUniversalTime();
        }

        public DateTime? MonthYear 
        { 
            get => _monthYear;
            set => _monthYear = value?.Kind == DateTimeKind.Unspecified ? 
                DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : 
                value?.ToUniversalTime();
        }

        public DateTime? CompletedDate 
        { 
            get => _completedDate;
            set => _completedDate = value?.Kind == DateTimeKind.Unspecified ? 
                DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : 
                value?.ToUniversalTime();
        }

        public int? LeaderId { get; set; }
        public Leader Leader { get; set; } = null!;

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
    }
}
