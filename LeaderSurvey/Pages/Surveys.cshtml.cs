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
            Surveys = await _context.Surveys
                .Include(s => s.Leader)
                .OrderByDescending(s => s.MonthYear)  // Changed from Date to MonthYear
                .ToListAsync();

            Leaders = await _context.Leaders.ToListAsync();
            LeaderSelectList = new SelectList(Leaders, "Id", "Name");

            AvailableDates = await _context.Surveys
                .Select(s => s.MonthYear ?? DateTime.Now)  // Changed from Date to MonthYear with null check
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();
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
                    ? DateTime.SpecifyKind(NewSurvey.MonthYear.Value, DateTimeKind.Utc) 
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
    }
}
