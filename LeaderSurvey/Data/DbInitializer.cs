using LeaderSurvey.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderSurvey.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();
            
            // Seed question categories if they don't exist
            if (!await context.QuestionCategories.AnyAsync())
            {
                var categories = new List<QuestionCategory>
                {
                    new QuestionCategory { Name = "Order Accuracy", Description = "Questions related to order accuracy" },
                    new QuestionCategory { Name = "Cleanliness", Description = "Questions related to cleanliness" },
                    new QuestionCategory { Name = "Attentive/Courteous", Description = "Questions related to attentiveness and courtesy" },
                    new QuestionCategory { Name = "Fast Service", Description = "Questions related to service speed" }
                };
                
                await context.QuestionCategories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }
        }
    }
}
