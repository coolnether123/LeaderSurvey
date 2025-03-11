﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeaderSurvey.Models
{
    public class Survey
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public string Area { get; set; } = string.Empty;

        public int? LeaderId { get; set; }
        public Leader? Leader { get; set; }

        private DateTime _date = DateTime.UtcNow;
        private DateTime? _monthYear;
        private DateTime? _completedDate;

        [Column(TypeName = "timestamp with time zone")]
        public DateTime Date
        {
            get => _date;
            set => _date = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
        
        [Column(TypeName = "timestamp with time zone")]
        public DateTime? MonthYear
        {
            get => _monthYear;
            set => _monthYear = value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
        }
        
        [Column(TypeName = "timestamp with time zone")]
        public DateTime? CompletedDate
        {
            get => _completedDate;
            set => _completedDate = value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
        }

        [NotMapped]
        public DateTime LocalDate 
        {
            get => Date.ToLocalTime();
            set => Date = value.ToUniversalTime();
        }

        [NotMapped]
        public DateTime? LocalMonthYear
        {
            get => MonthYear?.ToLocalTime();
            set => MonthYear = value?.ToUniversalTime();
        }

        [NotMapped]
        public DateTime? LocalCompletedDate
        {
            get => CompletedDate?.ToLocalTime();
            set => CompletedDate = value?.ToUniversalTime();
        }

        [Required]
        public string Status { get; set; } = "Pending";

        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
