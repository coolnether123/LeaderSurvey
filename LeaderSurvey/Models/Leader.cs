﻿using System.ComponentModel.DataAnnotations;

namespace LeaderSurvey.Models
{
    public class Leader
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Area { get; set; } = string.Empty;
        
        public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
    }
}
