using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LeaderSurvey.Pages
{
    public class NewSurveyModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public NewSurveyModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Survey { get; set; } = new();
        
        public SelectList LeaderSelectList { get; set; } = default!;
        public SelectList AreaSelectList { get; set; } = default!;

        public class InputModel
        {
            [Required(ErrorMessage = "Survey name is required")]
            public string Name { get; set; } = string.Empty;

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
            // Get all leaders from the database
            var leaders = await _context.Leaders
                .OrderBy(l => l.Name)
                .ToListAsync();
            
            LeaderSelectList = new SelectList(leaders, "Id", "Name");

            // Define the four valid areas
            var areas = new[] { "Front", "Drive", "Kitchen", "Hospitality" };
            AreaSelectList = new SelectList(areas);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Reload dropdowns
                return Page();
            }

            var survey = new Survey
            {
                Name = Survey.Name.Trim(),
                Description = Survey.Description?.Trim() ?? string.Empty,
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

            return RedirectToPage("/Surveys");
        }
    }
}