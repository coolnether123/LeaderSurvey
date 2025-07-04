// Pages/Questions.cshtml.cs
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LeaderSurvey.Pages
{
    public class QuestionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuestionsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Question NewQuestion { get; set; } = default!;

        public IList<Question> Questions { get; set; } = new List<Question>();
        public Survey? Survey { get; set; }
        public int SurveyId { get; set; }

        public async Task<IActionResult> OnGetAsync(int surveyId)
        {
            Questions = await _context.Questions
                .Where(q => q.SurveyId == surveyId)
                .OrderBy(q => q.QuestionOrder)
                .ToListAsync();

            Survey = await _context.Surveys
                .FirstOrDefaultAsync(s => s.Id == surveyId);
            
            if (Survey == null)
            {
                return NotFound();
            }

            SurveyId = surveyId;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int surveyId)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var questionCount = await _context.Questions
                .CountAsync(q => q.SurveyId == surveyId);

            NewQuestion.SurveyId = surveyId;
            NewQuestion.QuestionOrder = questionCount + 1;

            _context.Questions.Add(NewQuestion);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { surveyId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int surveyId)
        {
            var question = await _context.Questions.FindAsync(id);
            
            if (question != null)
            {
                _context.Questions.Remove(question);
                
                // Reorder remaining questions
                var remainingQuestions = await _context.Questions
                    .Where(q => q.SurveyId == surveyId && q.QuestionOrder > question.QuestionOrder)
                    .ToListAsync();

                foreach (var q in remainingQuestions)
                {
                    q.QuestionOrder--;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { surveyId });
        }
    }
}
