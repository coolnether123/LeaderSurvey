using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using LeaderSurvey.Extensions;

namespace LeaderSurvey.Pages
{
    public class PublicTakeSurveyModel(ApplicationDbContext context) : PageModel
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

        [BindProperty]
        public string? SurveyStatus { get; set; }

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
                StatusMessage = "This survey has already been completed. You cannot take it again.";
                return RedirectToPage("./ThankYou", new { message = StatusMessage });
            }

            Survey = survey;
            Questions = [.. Survey.Questions.OrderBy(q => q.QuestionOrder)];

            // Only show the leader assigned to the survey
            if (Survey.LeaderId.HasValue && Survey.Leader != null)
            {
                var leadersList = new List<Leader> { Survey.Leader };
                LeaderList = new SelectList(leadersList, "Id", "Name");
                SelectedLeaderId = Survey.LeaderId.Value;
            }
            else
            {
                // If no specific leader is assigned, show all leaders
                var leaders = await _context.Leaders.ToListAsync();
                LeaderList = new SelectList(leaders, "Id", "Name");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Ensure we have a valid survey ID
            if (SurveyId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid survey ID. Please try again.");
                return RedirectToPage("./ThankYou", new { message = "An error occurred. Please try again." });
            }

            try
            {
                // Validate that all questions have answers
                var allQuestions = await _context.Questions
                    .Where(q => q.SurveyId == SurveyId)
                    .ToListAsync();

                foreach (var question in allQuestions)
                {
                    if (Answers == null || !Answers.TryGetValue(question.Id, out var answer) || string.IsNullOrWhiteSpace(answer))
                    {
                        ModelState.AddModelError($"Answers[{question.Id}]", $"Please provide an answer for question: {question.Text}");
                    }
                }

                if (!ModelState.IsValid)
                {
                    // If AJAX, return JSON errors instead of the page
                    if (Request.IsAjaxRequest())
                    {
                        var errors = ModelState
                            .Where(m => m.Value?.Errors.Count > 0)
                            .ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                            );
                        return new JsonResult(new { success = false, errors }) { StatusCode = 400 };
                    }

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
                        var leaders = await _context.Leaders.ToListAsync();
                        LeaderList = new SelectList(leaders, "Id", "Name");
                    }

                    return Page();
                }

                // Create a single survey response
                var surveyResponse = new SurveyResponse
                {
                    SurveyId = SurveyId,
                    LeaderId = SelectedLeaderId,
                    AdditionalNotes = AdditionalNotes,
                    CompletionDate = DateTime.UtcNow
                };

                _context.SurveyResponses.Add(surveyResponse);
                await _context.SaveChangesAsync(); // Save to get the ID

                // Create individual answers
                var answers = new List<Answer>();
                foreach (var answer in Answers)
                {
                    var answerRecord = new Answer
                    {
                        QuestionId = answer.Key,
                        SurveyResponseId = surveyResponse.Id,
                        Response = answer.Value
                    };
                    answers.Add(answerRecord);
                }

                // Save all answers
                _context.Answers.AddRange(answers);

                // Update survey status to completed
                var surveyToUpdate = await _context.Surveys.FindAsync(SurveyId);
                if (surveyToUpdate != null)
                {
                    surveyToUpdate.Status = "Completed";
                    surveyToUpdate.CompletedDate = DateTime.UtcNow;
                    _context.Surveys.Update(surveyToUpdate);
                }

                await _context.SaveChangesAsync();

                // If AJAX request, return success JSON
                if (Request.IsAjaxRequest())
                {
                    return new JsonResult(new { success = true, message = "Survey submitted successfully. Thank you!" });
                }

                // Redirect to thank you page
                return RedirectToPage("./ThankYou", new { message = "Survey submitted successfully. Thank you!" });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while saving the survey: {ex.Message}");

                // If AJAX request, return error JSON
                if (Request.IsAjaxRequest())
                {
                    return new JsonResult(new { success = false, message = ex.Message }) { StatusCode = 500 };
                }

                // Reload the survey data for the page
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
                    var leaders = await _context.Leaders.ToListAsync();
                    LeaderList = new SelectList(leaders, "Id", "Name");
                }

                return Page();
            }
        }
    }
}
