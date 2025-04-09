using System.ComponentModel.DataAnnotations;

namespace LeaderSurvey.Models
{
    public class QuestionCategory
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        // Navigation property for the many-to-many relationship with questions
        public virtual ICollection<QuestionCategoryMapping> QuestionMappings { get; set; } = new List<QuestionCategoryMapping>();
    }
}
