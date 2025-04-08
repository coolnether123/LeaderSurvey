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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Survey -> Leader (being surveyed) relationship
            modelBuilder.Entity<Survey>()
                .HasOne(s => s.Leader)
                .WithMany(l => l.Surveys)
                .HasForeignKey(s => s.LeaderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Survey -> EvaluatorLeader (taking the survey) relationship
            modelBuilder.Entity<Survey>()
                .HasOne(s => s.EvaluatorLeader)
                .WithMany()
                .HasForeignKey(s => s.EvaluatorLeaderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Survey)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.SurveyId);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.SurveyResponse)
                .WithMany(sr => sr.Answers)
                .HasForeignKey(a => a.SurveyResponseId);

            // Configure SurveyResponse -> Leader relationship
            modelBuilder.Entity<SurveyResponse>()
                .HasOne(sr => sr.Leader)
                .WithMany()
                .HasForeignKey(sr => sr.LeaderId);

            modelBuilder.Entity<SurveyResponse>()
                .HasOne(sr => sr.Survey)
                .WithMany()
                .HasForeignKey(sr => sr.SurveyId);
        }
    }
}