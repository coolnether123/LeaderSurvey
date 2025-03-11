using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LeaderSurvey.Pages
{
    public class NewSurveyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NewSurveyModel> _logger;
        public List<Leader> Leaders { get; set; } = new();

        public NewSurveyModel(ApplicationDbContext context, ILogger<NewSurveyModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Survey { get; set; } = new();
        
        public SelectList LeaderSelectList { get; set; } = default!;
        public SelectList AreaSelectList { get; set; } = default!;

        public class InputModel
        {
            [Required(ErrorMessage = "Survey name is required")]
            public string Name { get; set; } = string.Empty;

            // Remove Required attribute if Description is optional
            // [Required]  // This might be causing the issue
            public string Description { get; set; } = string.Empty;

            [Required(ErrorMessage = "Area is required")]
            // Valid areas: Front, Drive, Kitchen, Hospitality
            public string Area { get; set; } = string.Empty;

            [Required(ErrorMessage = "Leader is required")]
            public int LeaderId { get; set; }

            [Required(ErrorMessage = "Month/Year is required")]
            public DateTime? MonthYear { get; set; }

            [Required(ErrorMessage = "At least one question is required")]
            public List<QuestionModel> Questions { get; set; } = new();
        }

        public class QuestionModel
        {
            [Required(ErrorMessage = "Question text is required")]
            public string Text { get; set; } = string.Empty;
            
            [Required(ErrorMessage = "Question type is required")]
            public string QuestionType { get; set; } = "yesno"; // yesno or score
        }

        public async Task OnGetAsync()
        {
            // Load leaders directly into the property
            Leaders = await _context.Leaders
                .OrderBy(l => l.Name)
                .ToListAsync();

            // Define the four valid areas
            var areas = new[] { "Front", "Drive", "Kitchen", "Hospitality" };
            AreaSelectList = new SelectList(areas);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Log the received data
                _logger.LogInformation($"Received survey data: Name={Survey.Name}, Area={Survey.Area}, LeaderId={Survey.LeaderId}");
                _logger.LogInformation($"Questions count: {Survey.Questions?.Count ?? 0}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                        );

                    _logger.LogWarning($"Validation failed: {JsonSerializer.Serialize(errors)}");
                    return BadRequest(new { errors = errors });
                }

                // Create the survey
                var survey = new Survey
                {
                    Name = Survey.Name.Trim(),
                    Description = Survey.Description?.Trim() ?? string.Empty, // Handle null Description
                    Area = Survey.Area,
                    LeaderId = Survey.LeaderId,
                    MonthYear = Survey.MonthYear.HasValue 
                        ? DateTime.SpecifyKind(Survey.MonthYear.Value, DateTimeKind.Utc) 
                        : null,
                    Date = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                    Status = "Draft"
                };

                _context.Surveys.Add(survey);
                await _context.SaveChangesAsync();

                // Add questions
                if (Survey.Questions != null)
                {
                    foreach (var q in Survey.Questions)
                    {
                        var question = new Question
                        {
                            SurveyId = survey.Id,
                            Text = q.Text.Trim(),
                            QuestionType = q.QuestionType,
                            QuestionOrder = Survey.Questions.IndexOf(q) + 1
                        };
                        _context.Questions.Add(question);
                    }
                    await _context.SaveChangesAsync();
                }

                return new JsonResult(new { success = true, redirectTo = "/Surveys" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating survey");
                return BadRequest(new { 
                    errors = new Dictionary<string, List<string>> {
                        { "general", new List<string> { "An error occurred while saving the survey" } }
                    }
                });
            }
        }
    }
}