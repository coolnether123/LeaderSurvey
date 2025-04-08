using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using System.ComponentModel.DataAnnotations;

namespace LeaderSurvey.Pages
{
    public class TakeSurveyModel(ApplicationDbContext context) : PageModel
    {
        private readonly ApplicationDbContext _context = context;

        public Survey Survey { get; set; } = new();
        public List<Question> Questions { get; set; } = [];
        public required SelectList LeaderList { get; set; }

        [BindProperty]
        public int SurveyId { get; set; }

        [BindProperty, Required(ErrorMessage = "Please select a leader to evaluate")]
        public int SelectedLeaderId { get; set; }

        [BindProperty]
        public Dictionary<int, string> Answers { get; set; } = [];

        [BindProperty]
        public string? AdditionalNotes { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? surveyId)
        {
            if (surveyId == null)
            {
                return NotFound();
            }

            SurveyId = surveyId.Value;
            var survey = await _context.Surveys
                .Include(s => s.Questions)
                .Include(s => s.Leader)
                .Include(s => s.EvaluatorLeader)
                .FirstOrDefaultAsync(s => s.Id == surveyId);

            if (survey == null)
            {
                return NotFound();
            }

            // Check if survey is already completed
            if (survey.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "This survey has already been completed.";
                return RedirectToPage("./Surveys");
            }

            Survey = survey;
            Questions = [.. Survey.Questions.OrderBy(q => q.QuestionOrder)];

            // Only show the leader assigned to the survey
            if (survey.LeaderId.HasValue && survey.Leader != null)
            {
                var leadersList = new List<Leader> { survey.Leader };
                LeaderList = new SelectList(leadersList, "Id", "Name");
                SelectedLeaderId = survey.LeaderId.Value;
            }
            else
            {
                // Fallback to showing leaders in the same area if no leader is assigned
                var leaders = await _context.Leaders
                    .Where(l => l.Area == survey.Area)
                    .OrderBy(l => l.Name)
                    .ToListAsync();
                LeaderList = new SelectList(leaders, "Id", "Name");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validate that all questions have answers
            var allQuestions = await _context.Questions
                .Where(q => q.SurveyId == SurveyId)
                .ToListAsync();

            foreach (var question in allQuestions)
            {
                if (!Answers.TryGetValue(question.Id, out var answer) || string.IsNullOrWhiteSpace(answer))
                {
                    ModelState.AddModelError($"Answers[{question.Id}]", $"Please provide an answer for question: {question.Text}");
                }
            }

            if (!ModelState.IsValid)
            {
                var surveyResult = await _context.Surveys
                    .Include(s => s.Questions)
                    .Include(s => s.Leader)
                    .Include(s => s.EvaluatorLeader)
                    .FirstOrDefaultAsync(s => s.Id == SurveyId);

                if (surveyResult != null)
                {
                    Survey = surveyResult;
                    Questions = [.. Survey.Questions.OrderBy(q => q.QuestionOrder)];
                }

                // Only show the leader assigned to the survey
                if (Survey.LeaderId.HasValue && Survey.Leader != null)
                {
                    var leadersList = new List<Leader> { Survey.Leader };
                    LeaderList = new SelectList(leadersList, "Id", "Name");
                    SelectedLeaderId = Survey.LeaderId.Value;
                }
                else
                {
                    // Fallback to showing leaders in the same area if no leader is assigned
                    var leaders = await _context.Leaders
                        .Where(l => l.Area == Survey.Area)
                        .OrderBy(l => l.Name)
                        .ToListAsync();
                    LeaderList = new SelectList(leaders, "Id", "Name");
                }
                return Page();
            }

            // Create the survey response
            var surveyResponse = new SurveyResponse
            {
                LeaderId = SelectedLeaderId,
                SurveyId = SurveyId,
                CompletionDate = DateTime.UtcNow,
                AdditionalNotes = AdditionalNotes
            };

            _context.SurveyResponses.Add(surveyResponse);
            await _context.SaveChangesAsync();

            // Add all answers
            foreach (var answer in Answers)
            {
                _context.Answers.Add(new Answer
                {
                    QuestionId = answer.Key,
                    SurveyResponseId = surveyResponse.Id,
                    Response = answer.Value
                });
            }

            // Update the survey status to completed
            var survey = await _context.Surveys.FindAsync(SurveyId);
            if (survey != null)
            {
                survey.Status = "Completed";
                survey.CompletedDate = DateTime.UtcNow;
                survey.LeaderId = SelectedLeaderId;
                _context.Surveys.Update(survey);
            }

            await _context.SaveChangesAsync();
            StatusMessage = "Survey completed successfully. Thank you for your feedback!";
            return Redirect("/Surveys");
        }
    }
}
