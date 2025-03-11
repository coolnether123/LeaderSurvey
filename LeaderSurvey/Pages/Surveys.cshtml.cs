using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using LeaderSurvey.Data;
using LeaderSurvey.Models;

namespace LeaderSurvey.Pages
{
    public class SurveysModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SurveysModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Survey> Surveys { get; set; } = new List<Survey>();
        public SelectList LeaderSelectList { get; set; } = new SelectList(Enumerable.Empty<Leader>(), "Id", "Name");
        public List<Leader> Leaders { get; set; } = new();
        
        public string? FilteredLeaderName { get; set; }
        public bool IsFiltered => HttpContext.Request.Query.ContainsKey("leaderId");

        [BindProperty]
        public InputModel NewSurvey { get; set; } = new InputModel();
        
        [BindProperty]
        public Survey SelectedSurvey { get; set; } = new Survey { Name = "", Description = "", Area = "" };

        public class InputModel
        {
            [Required]
            public string Name { get; set; }

            public string Description { get; set; }
            
            public string Area { get; set; }
            
            public int? LeaderId { get; set; }
            
            public DateTime? MonthYear { get; set; }
        }

        public async Task OnGetAsync(int? id, int? leaderId)
        {
            // Load all leaders first
            Leaders = await _context.Leaders.ToListAsync();

            // Create a query that includes leader information
            IQueryable<Survey> query = _context.Surveys.Include(s => s.Leader);
            
            // Apply leader filter if provided
            if (leaderId.HasValue)
            {
                query = query.Where(s => s.LeaderId == leaderId);
                FilteredLeaderName = Leaders.FirstOrDefault(l => l.Id == leaderId)?.Name;
            }
            
            // Execute query and store results
            Surveys = await query.ToListAsync();
            
            // Load leaders for dropdown
            await LoadLeaderSelectListAsync();
            
            // Initialize properties to avoid null reference when binding
            NewSurvey = new InputModel();
            
            if (id.HasValue)
            {
                SelectedSurvey = await _context.Surveys.FirstOrDefaultAsync(m => m.Id == id) 
                    ?? new Survey { Name = "", Description = "", Area = "" };
            }
            else
            {
                SelectedSurvey = new Survey { Name = "", Description = "", Area = "" };
            }
        }
        
        private async Task LoadLeaderSelectListAsync()
        {
            var leaders = await _context.Leaders.OrderBy(l => l.Name).ToListAsync();
            LeaderSelectList = new SelectList(leaders, "Id", "Name");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Surveys = await _context.Surveys.Include(s => s.Leader).ToListAsync();
                await LoadLeaderSelectListAsync();
                return Page();
            }

            // Create new survey with all properties
            var survey = new Survey 
            { 
                Name = NewSurvey.Name, 
                Description = NewSurvey.Description,
                Area = NewSurvey.Area,
                LeaderId = NewSurvey.LeaderId,
                MonthYear = NewSurvey.MonthYear
            };
            
            // If no area is provided but a leader is selected, use the leader's area
            if (string.IsNullOrEmpty(survey.Area) && survey.LeaderId.HasValue)
            {
                var leader = await _context.Leaders.FindAsync(survey.LeaderId.Value);
                if (leader != null)
                {
                    survey.Area = leader.Area;
                }
            }
            
            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Surveys");
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var survey = await _context.Surveys.FindAsync(id);
            if (survey != null)
            {
                //This deletes all questions attached to it.
                var questions = _context.Questions.Where(c => c.SurveyId == id);
                foreach (var question in questions)
                {
                    _context.Questions.Remove(question);
                }
                _context.Surveys.Remove(survey);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Surveys");
        }
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                // If model state is invalid, return to the page with validation errors
                Surveys = await _context.Surveys.Include(s => s.Leader).ToListAsync();
                await LoadLeaderSelectListAsync();
                return Page();
            }

            // If no area is provided but a leader is selected, use the leader's area
            if (string.IsNullOrEmpty(SelectedSurvey.Area) && SelectedSurvey.LeaderId.HasValue)
            {
                var leader = await _context.Leaders.FindAsync(SelectedSurvey.LeaderId.Value);
                if (leader != null)
                {
                    SelectedSurvey.Area = leader.Area;
                }
            }

            _context.Attach(SelectedSurvey).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Surveys.Any(l => l.Id == SelectedSurvey.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Surveys");
        }
    }
}
