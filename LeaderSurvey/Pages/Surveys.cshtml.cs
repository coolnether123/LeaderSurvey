using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using System.ComponentModel.DataAnnotations;

namespace LeaderSurvey.Pages
{
    public class SurveysModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SurveysModel(ApplicationDbContext context)
        {
            _context = context;
            // Initialize all properties
            Surveys = new List<Survey>();
            Leaders = new List<Leader>();
            AvailableDates = new List<DateTime>();
            LeaderSelectList = new SelectList(Enumerable.Empty<Leader>());
            NewSurvey = new InputModel();
            SelectedSurvey = new Survey();
        }

        public IList<Survey> Surveys { get; set; }
        public SelectList LeaderSelectList { get; set; }
        public List<Leader> Leaders { get; set; }
        public List<DateTime> AvailableDates { get; set; }
        public List<QuestionCategory> Categories { get; set; } = new();

        [BindProperty]
        public InputModel NewSurvey { get; set; }

        [BindProperty]
        public Survey SelectedSurvey { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Name is required")]
            public string Name { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            [Required(ErrorMessage = "Area is required")]
            public string Area { get; set; } = string.Empty;

            public int? LeaderId { get; set; }

            public DateTime? MonthYear { get; set; }
        }

        public async Task OnGetAsync()
        {
            // Load surveys with explicit UTC conversion
            var rawSurveys = await _context.Surveys
                .Include(s => s.Leader)
                .ToListAsync();

            // Ensure all dates are in UTC before sorting and fix timezone issues
            Surveys = rawSurveys
                .Select(s => {
                    if (s.MonthYear.HasValue)
                    {
                        s.MonthYear = DateTime.SpecifyKind(s.MonthYear.Value, DateTimeKind.Utc);
                    }
                    return s;
                })
                .OrderByDescending(s => s.MonthYear ?? DateTime.MinValue)
                .ToList();

            Leaders = await _context.Leaders.ToListAsync();
            LeaderSelectList = new SelectList(Leaders, "Id", "Name");

            // Load categories
            Categories = await _context.QuestionCategories
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Get distinct dates with explicit UTC handling
            AvailableDates = rawSurveys
                .Where(s => s.MonthYear.HasValue)
                .Select(s => DateTime.SpecifyKind(s.MonthYear!.Value, DateTimeKind.Utc))
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var survey = new Survey
            {
                Name = NewSurvey.Name,
                Description = NewSurvey.Description,
                Area = NewSurvey.Area,
                LeaderId = NewSurvey.LeaderId,
                MonthYear = NewSurvey.MonthYear.HasValue
                    ? new DateTime(NewSurvey.MonthYear.Value.Year, NewSurvey.MonthYear.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                    : null,
                Date = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                Status = "Pending"
            };

            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var survey = await _context.Surveys.FindAsync(SelectedSurvey.Id);
            if (survey == null)
            {
                return NotFound();
            }

            survey.Name = SelectedSurvey.Name;
            survey.Description = SelectedSurvey.Description;
            survey.Area = SelectedSurvey.Area;
            survey.LeaderId = SelectedSurvey.LeaderId;
            survey.MonthYear = SelectedSurvey.MonthYear;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Surveys.Any(s => s.Id == SelectedSurvey.Id))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var survey = await _context.Surveys.FindAsync(id);
            if (survey != null)
            {
                _context.Surveys.Remove(survey);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReEnableAsync([FromBody] System.Text.Json.JsonElement body)
        {
            try
            {
                var id = body.GetProperty("id").GetInt32();
                var survey = await _context.Surveys.FindAsync(id);

                if (survey == null)
                {
                    return new JsonResult(new { success = false, message = "Survey not found" });
                }

                // Reset the survey status to Active and clear completion date
                survey.Status = "Active";
                survey.CompletedDate = null;

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Survey re-enabled successfully" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error re-enabling survey: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnGetSurveyDetailsAsync(int id)
        {
            var survey = await _context.Surveys
                .Include(s => s.Leader)
                .Include(s => s.EvaluatorLeader)
                .Include(s => s.Questions)
                    .ThenInclude(q => q.CategoryMappings)
                        .ThenInclude(cm => cm.Category)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
            {
                return NotFound();
            }

            var surveyResponses = await _context.SurveyResponses
                .Include(sr => sr.Answers)
                .Where(sr => sr.SurveyId == id)
                .ToListAsync();

            var responseData = new
            {
                survey,
                responses = surveyResponses.Select(sr => new
                {
                    completionDate = sr.CompletionDate,
                    additionalNotes = sr.AdditionalNotes,
                    answers = sr.Answers.Select(a => new
                    {
                        questionId = a.QuestionId,
                        response = a.Response
                    })
                })
            };

            return new JsonResult(responseData);
        }
    }
}
