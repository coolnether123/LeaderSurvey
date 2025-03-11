using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LeaderSurvey.Data;
using LeaderSurvey.Models;

namespace LeaderSurvey.Pages
{
    public class TakeSurveyModel(ApplicationDbContext context) : PageModel
    {
        private readonly ApplicationDbContext _context = context;

        public Survey Survey { get; set; } = new();
        public List<Question> Questions { get; set; } = new();
        public required SelectList LeaderList { get; set; }

        [BindProperty]
        public int SurveyId { get; set; }

        [BindProperty]
        public int SelectedLeaderId { get; set; }

        [BindProperty]
        public Dictionary<int, string> Answers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            SurveyId = id.Value;
            var survey = await _context.Surveys
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
            {
                return NotFound();
            }

            Survey = survey;
            Questions = Survey.Questions.OrderBy(q => q.QuestionOrder).ToList();

            var leaders = await _context.Leaders.ToListAsync();
            LeaderList = new SelectList(leaders, "Id", "Name");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var surveyResult = await _context.Surveys
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync(s => s.Id == SurveyId);
                
                if (surveyResult != null)
                {
                    Survey = surveyResult;
                    Questions = Survey.Questions.OrderBy(q => q.QuestionOrder).ToList();
                }
                
                var leaders = await _context.Leaders.ToListAsync();
                LeaderList = new SelectList(leaders, "Id", "Name");
                return Page();
            }

            var surveyResponse = new SurveyResponse
            {
                LeaderId = SelectedLeaderId,
                SurveyId = SurveyId,
                CompletionDate = DateTime.UtcNow
            };

            _context.SurveyResponses.Add(surveyResponse);
            await _context.SaveChangesAsync();

            foreach (var answer in Answers)
            {
                _context.Answers.Add(new Answer
                {
                    QuestionId = answer.Key,
                    SurveyResponseId = surveyResponse.Id,
                    Response = answer.Value
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}
