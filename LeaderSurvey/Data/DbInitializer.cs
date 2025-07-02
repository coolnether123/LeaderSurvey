using LeaderSurvey.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaderSurvey.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            // Create a logger for debugging
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger("DbInitializer");

            logger.LogInformation("Starting database initialization...");

            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");

            // Seed roles
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Admin role created successfully.");
                }
                else
                {
                    logger.LogError("Failed to create Admin role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Admin role already exists.");
            }

            // Seed admin user
            var adminEmail = configuration["AdminUser:Email"];
            var adminPassword = configuration["AdminUser:Password"];

            logger.LogInformation("Attempting to create admin user with email: {Email}", adminEmail);
            logger.LogInformation("Admin password from config: {Password}", adminPassword?.Substring(0, Math.Min(3, adminPassword.Length)) + "***");

            var existingUser = await userManager.FindByEmailAsync(adminEmail);
            if (existingUser == null)
            {
                logger.LogInformation("Admin user not found, creating new user...");

                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    logger.LogInformation("Admin user created successfully.");

                    var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation("Admin role assigned to user successfully.");
                    }
                    else
                    {
                        logger.LogError("Failed to assign Admin role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Admin user already exists: {Email}, UserName: {UserName}, EmailConfirmed: {EmailConfirmed}",
                    existingUser.Email, existingUser.UserName, existingUser.EmailConfirmed);

                // Check if user has admin role
                var isInRole = await userManager.IsInRoleAsync(existingUser, "Admin");
                logger.LogInformation("User is in Admin role: {IsInRole}", isInRole);

                // Verify password
                var passwordValid = await userManager.CheckPasswordAsync(existingUser, adminPassword);
                logger.LogInformation("Password validation for existing user: {PasswordValid}", passwordValid);
            }

            // Log total user count
            var totalUsers = userManager.Users.Count();
            logger.LogInformation("Total users in database: {UserCount}", totalUsers);

            // Seed question categories if they don't exist
            if (!await context.QuestionCategories.AnyAsync())
            {
                logger.LogInformation("Seeding question categories...");
                var categories = new List<QuestionCategory>
                {
                    new QuestionCategory { Name = "Mechanical", Description = "Questions related to mechanical skills and processes" },
                    new QuestionCategory { Name = "Character", Description = "Questions related to character traits and behaviors" },
                    new QuestionCategory { Name = "Theory", Description = "Questions related to theoretical knowledge and understanding" }
                };

                await context.QuestionCategories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
                logger.LogInformation("Question categories seeded successfully.");
            }
            else
            {
                logger.LogInformation("Question categories already exist.");
            }

            logger.LogInformation("Database initialization completed.");
        }
    }
}