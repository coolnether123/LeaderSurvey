using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using System.ComponentModel.DataAnnotations;
using LeaderSurvey.Extensions;

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

            // Redirect to the public-facing survey page
            return RedirectToPage("/PublicTakeSurvey", new { surveyId = surveyId.Value });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Debug information
            System.Diagnostics.Debug.WriteLine($"OnPostAsync called. SurveyId: {SurveyId}, SelectedLeaderId: {SelectedLeaderId}");
            System.Diagnostics.Debug.WriteLine($"Answers count: {Answers?.Count ?? 0}, AdditionalNotes: {(string.IsNullOrEmpty(AdditionalNotes) ? "<none>" : AdditionalNotes)}");

            // Ensure we have a valid survey ID
            if (SurveyId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid survey ID. Please try again.");
                return RedirectToPage("/Surveys");
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
                        // Fallback to showing leaders in the same area if no leader is assigned
                        var leaders = await _context.Leaders
                            .Where(l => l.Area == Survey.Area)
                            .OrderBy(l => l.Name)
                            .ToListAsync();
                        LeaderList = new SelectList(leaders, "Id", "Name");
                    }
                    return Page();
                }

                // Get the current time for completion timestamp
                var completionTime = DateTime.UtcNow;

                // Log before creating survey response
                System.Diagnostics.Debug.WriteLine($"Creating survey response for SurveyId: {SurveyId}, LeaderId: {SelectedLeaderId}");

                // Create the survey response
                var surveyResponse = new SurveyResponse
                {
                    LeaderId = SelectedLeaderId,
                    SurveyId = SurveyId,
                    CompletionDate = completionTime,
                    AdditionalNotes = AdditionalNotes
                };

                _context.SurveyResponses.Add(surveyResponse);

                try {
                    await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Successfully saved survey response with ID: {surveyResponse.Id}");
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Error saving survey response: {ex.Message}");
                    throw; // Re-throw to be caught by the outer try-catch
                }

                // Add all answers
                if (Answers != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Adding {Answers.Count} answers to survey response {surveyResponse.Id}");

                    foreach (var answer in Answers)
                    {
                        System.Diagnostics.Debug.WriteLine($"Adding answer for QuestionId: {answer.Key}, Response: {answer.Value}");

                        _context.Answers.Add(new Answer
                        {
                            QuestionId = answer.Key,
                            SurveyResponseId = surveyResponse.Id,
                            Response = answer.Value
                        });
                    }

                    try {
                        await _context.SaveChangesAsync();
                        System.Diagnostics.Debug.WriteLine("Successfully saved all answers");
                    } catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine($"Error saving answers: {ex.Message}");
                        throw; // Re-throw to be caught by the outer try-catch
                    }
                }

                // Update the survey status to completed
                var survey = await _context.Surveys.FindAsync(SurveyId);
                if (survey != null)
                {
                    // Use the SurveyStatus field if provided, otherwise default to "Completed"
                    string newStatus = !string.IsNullOrEmpty(SurveyStatus) ? SurveyStatus : "Completed";
                    System.Diagnostics.Debug.WriteLine($"Updating survey {SurveyId} status to {newStatus}");
                    survey.Status = newStatus;
                    survey.CompletedDate = completionTime;
                    survey.LeaderId = SelectedLeaderId;
                    _context.Surveys.Update(survey);

                    try {
                        await _context.SaveChangesAsync();
                        System.Diagnostics.Debug.WriteLine($"Successfully updated survey status to {newStatus}");
                    } catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine($"Error updating survey status: {ex.Message}");
                        throw; // Re-throw to be caught by the outer try-catch
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"WARNING: Survey with ID {SurveyId} not found when trying to update status");
                }
                StatusMessage = "Survey completed successfully. Thank you for your feedback!";

                // Check if this is an AJAX request
                if (Request.IsAjaxRequest())
                {
                    // Return a JSON response for AJAX requests
                    return new JsonResult(new { success = true, message = "Survey saved. Thank you!" });
                }

                // For non-AJAX requests, redirect to the Surveys page
                return Redirect("/Surveys");
            }
            catch (Exception ex)
            {
                // Log the exception with detailed information
                System.Diagnostics.Debug.WriteLine($"Exception in OnPostAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Log inner exception if available
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }

                // Log model state errors
                foreach (var modelState in ModelState)
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Model error for {modelState.Key}: {error.ErrorMessage}");
                    }
                }

                ModelState.AddModelError(string.Empty, $"An error occurred while saving the survey: {ex.Message}");

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
                    // Fallback to showing leaders in the same area if no leader is assigned
                    var leaders = await _context.Leaders
                        .Where(l => l.Area == Survey.Area)
                        .OrderBy(l => l.Name)
                        .ToListAsync();
                    LeaderList = new SelectList(leaders, "Id", "Name");
                }

                // Check if this is an AJAX request
                if (Request.IsAjaxRequest())
                {
                    // Return a JSON response with validation errors
                    var errors = ModelState
                        .Where(m => m.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                        );

                    // Log the validation errors for debugging
                    System.Diagnostics.Debug.WriteLine($"Validation errors: {errors.Count}");
                    foreach (var error in errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  {error.Key}: {string.Join(", ", error.Value)}");
                    }

                    return new JsonResult(new { success = false, errors }) { StatusCode = 400 };
                }

                // For non-AJAX requests, return the page with validation errors
                return Page();
            }
        }
    }
}
