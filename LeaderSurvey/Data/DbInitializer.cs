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
                    new QuestionCategory { Name = "Mechanical", Description = "Questions related to mechanical skills and processes" },
                    new QuestionCategory { Name = "Character", Description = "Questions related to character traits and behaviors" },
                    new QuestionCategory { Name = "Theory", Description = "Questions related to theoretical knowledge and understanding" }
                };

                await context.QuestionCategories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }
            else
            {
                // Update existing categories if needed
                var existingCategories = await context.QuestionCategories.ToListAsync();
                var categoryNames = new[] { "Mechanical", "Character", "Theory" };
                var categoryDescriptions = new[] {
                    "Questions related to mechanical skills and processes",
                    "Questions related to character traits and behaviors",
                    "Questions related to theoretical knowledge and understanding"
                };

                // If we have the old categories, update them
                if (existingCategories.Count == 4 &&
                    existingCategories.Any(c => c.Name == "Order Accuracy") &&
                    existingCategories.Any(c => c.Name == "Cleanliness") &&
                    existingCategories.Any(c => c.Name == "Attentive/Courteous") &&
                    existingCategories.Any(c => c.Name == "Fast Service"))
                {
                    // Update the first 3 categories and remove the 4th
                    for (int i = 0; i < 3; i++)
                    {
                        existingCategories[i].Name = categoryNames[i];
                        existingCategories[i].Description = categoryDescriptions[i];
                    }

                    // Remove the 4th category if it exists
                    if (existingCategories.Count > 3)
                    {
                        context.QuestionCategories.Remove(existingCategories[3]);
                    }

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
