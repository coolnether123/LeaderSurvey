namespace LeaderSurvey.Models
{
    public class QuestionCategoryMapping
    {
        public int Id { get; set; }
        
        public int QuestionId { get; set; }
        public virtual Question? Question { get; set; }
        
        public int CategoryId { get; set; }
        public virtual QuestionCategory? Category { get; set; }
    }
}
