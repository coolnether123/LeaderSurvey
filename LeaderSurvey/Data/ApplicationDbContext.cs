using Microsoft.EntityFrameworkCore;
using LeaderSurvey.Models;

namespace LeaderSurvey.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // Remove the legacy timestamp behavior switch
        }

        public DbSet<Leader> Leaders => Set<Leader>();
        public DbSet<Survey> Surveys => Set<Survey>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
        public DbSet<Answer> Answers => Set<Answer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure all DateTime properties to use timestamp with time zone
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("timestamp with time zone");
                    }
                }
            }

            // Configure relationships
            modelBuilder.Entity<Survey>()
                .HasOne(s => s.Leader)
                .WithMany(l => l.Surveys)
                .HasForeignKey(s => s.LeaderId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SurveyResponse>()
                .HasOne(sr => sr.Survey)
                .WithMany()
                .HasForeignKey(sr => sr.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SurveyResponse>()
                .HasOne(sr => sr.Leader)
                .WithMany()
                .HasForeignKey(sr => sr.LeaderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.SurveyResponse)
                .WithMany(sr => sr.Answers)
                .HasForeignKey(a => a.SurveyResponseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}