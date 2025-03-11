﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeaderSurvey.Models
{
    public class Survey
    {
        public Survey()
        {
            Questions = new HashSet<Question>();
        }

        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public string Area { get; set; } = string.Empty;

        public int? LeaderId { get; set; }
        public Leader? Leader { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? MonthYear { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? CompletedDate { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public virtual ICollection<Question> Questions { get; set; }
    }
}
