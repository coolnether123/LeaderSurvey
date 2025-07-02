using Microsoft.EntityFrameworkCore;
using LeaderSurvey.Models;

namespace LeaderSurvey.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Survey> Surveys { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Leader> Leaders { get; set; }
        public DbSet<SurveyResponse> SurveyResponses { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<QuestionCategory> QuestionCategories { get; set; }
        public DbSet<QuestionCategoryMapping> QuestionCategoryMappings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Survey -> Leader (being surveyed) relationship
            modelBuilder.Entity<Survey>()
                .HasOne(s => s.Leader)
                .WithMany(l => l.Surveys) // This is correct because Leader.cs has ICollection<Survey>
                .HasForeignKey(s => s.LeaderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Survey -> EvaluatorLeader (taking the survey) relationship
            modelBuilder.Entity<Survey>()
                .HasOne(s => s.EvaluatorLeader)
                .WithMany() // **FIX #1: Re-added .WithMany() with empty parameters**
                .HasForeignKey(s => s.EvaluatorLeaderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Survey)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.SurveyResponse)
                .WithMany(sr => sr.Answers)
                .HasForeignKey(a => a.SurveyResponseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure SurveyResponse -> Leader relationship
            modelBuilder.Entity<SurveyResponse>()
                .HasOne(sr => sr.Leader)
                .WithMany() // **FIX #2: Re-added .WithMany() with empty parameters**
                .HasForeignKey(sr => sr.LeaderId);

            modelBuilder.Entity<SurveyResponse>()
                .HasOne(sr => sr.Survey)
                .WithMany() // This is also correct
                .HasForeignKey(sr => sr.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure QuestionCategoryMapping relationships
            modelBuilder.Entity<QuestionCategoryMapping>()
                .HasOne(qcm => qcm.Question)
                .WithMany(q => q.CategoryMappings)
                .HasForeignKey(qcm => qcm.QuestionId);

            modelBuilder.Entity<QuestionCategoryMapping>()
                .HasOne(qcm => qcm.Category)
                .WithMany(c => c.QuestionMappings)
                .HasForeignKey(qcm => qcm.CategoryId);
        }
    }
}