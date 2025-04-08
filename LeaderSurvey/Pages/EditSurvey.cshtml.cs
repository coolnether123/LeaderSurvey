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
    public class EditSurveyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditSurveyModel> _logger;
        public List<Leader> Leaders { get; set; } = new();

        public EditSurveyModel(ApplicationDbContext context, ILogger<EditSurveyModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Survey { get; set; } = new();

        [BindProperty]
        public bool IsViewMode { get; set; }

        [BindProperty]
        public string QuestionsJson { get; set; } = string.Empty;

        public SelectList LeaderSelectList { get; set; } = default!;
        public SelectList AreaSelectList { get; set; } = default!;

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Survey name is required")]
            public string Name { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            [Required(ErrorMessage = "Area is required")]
            public string Area { get; set; } = string.Empty;

            [Required(ErrorMessage = "Leader being surveyed is required")]
            [Display(Name = "Leader Being Surveyed")]
            public int LeaderId { get; set; }

            [Required(ErrorMessage = "Leader taking the survey is required")]
            [Display(Name = "Leader Taking Survey")]
            public int EvaluatorLeaderId { get; set; }

            [Required(ErrorMessage = "Month/Year is required")]
            public DateTime? MonthYear { get; set; }

            public List<QuestionModel> Questions { get; set; } = new();
        }

        public class QuestionModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Question text is required")]
            public string Text { get; set; } = string.Empty;

            [Required(ErrorMessage = "Question type is required")]
            public string QuestionType { get; set; } = "yesno"; // yesno or score
        }

        public async Task<IActionResult> OnGetAsync(int id, bool viewMode = false)
        {
            IsViewMode = viewMode;

            try
            {
                // Load the survey with questions - using separate queries to avoid potential issues
                var survey = await _context.Surveys.FindAsync(id);
                if (survey == null)
                {
                    TempData["ErrorMessage"] = $"Survey with ID {id} not found";
                    return RedirectToPage("/Surveys");
                }

                // Load questions separately
                _logger.LogInformation($"Loading questions for survey ID {id}");
                var questions = await _context.Questions
                    .Where(q => q.SurveyId == id)
                    .OrderBy(q => q.QuestionOrder)
                    .ToListAsync();

                _logger.LogInformation($"Found {questions.Count} questions in the database for survey ID {id}");
                foreach (var q in questions)
                {
                    _logger.LogInformation($"Database question - ID: {q.Id}, SurveyId: {q.SurveyId}, Text: {q.Text}, Type: {q.QuestionType}");
                }

                survey.Questions = questions;

                // Load leader separately if needed
                if (survey.LeaderId.HasValue)
                {
                    survey.Leader = await _context.Leaders.FindAsync(survey.LeaderId.Value);
                }

                // Load evaluator leader separately if needed
                if (survey.EvaluatorLeaderId.HasValue)
                {
                    survey.EvaluatorLeader = await _context.Leaders.FindAsync(survey.EvaluatorLeaderId.Value);
                }

                // Log the questions being loaded
                _logger.LogInformation($"Loading {survey.Questions.Count} questions for survey ID {id}");
                foreach (var q in survey.Questions)
                {
                    _logger.LogInformation($"Loading question ID: {q.Id}, Text: {q.Text}, Type: {q.QuestionType}");
                }

                // Map the survey to the input model
                Survey = new InputModel
                {
                    Id = survey.Id,
                    Name = survey.Name,
                    Description = survey.Description ?? string.Empty,
                    Area = survey.Area,
                    LeaderId = survey.LeaderId ?? 0,
                    EvaluatorLeaderId = survey.EvaluatorLeaderId ?? 0,
                    MonthYear = survey.MonthYear,
                    Questions = survey.Questions.Select(q => new QuestionModel
                    {
                        Id = q.Id,
                        Text = q.Text,
                        QuestionType = q.QuestionType
                    }).ToList()
                };

                // Log the mapped questions
                _logger.LogInformation($"Mapped {Survey.Questions.Count} questions to the input model");
                foreach (var q in Survey.Questions)
                {
                    _logger.LogInformation($"Mapped question ID: {q.Id}, Text: {q.Text}, Type: {q.QuestionType}");
                }

                // Load leaders
                Leaders = await _context.Leaders
                    .OrderBy(l => l.Name)
                    .ToListAsync();

                // Define the four valid areas
                var areas = new[] { "Front", "Drive", "Kitchen", "Hospitality" };
                AreaSelectList = new SelectList(areas);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading survey for editing");
                TempData["ErrorMessage"] = $"Error loading survey: {ex.Message}";
                return RedirectToPage("/Surveys");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // If in view mode, just redirect back to surveys
                if (IsViewMode)
                {
                    return RedirectToPage("/Surveys");
                }

                // Log the received data
                _logger.LogInformation($"Received survey data: Id={Survey.Id}, Name={Survey.Name}, Area={Survey.Area}, LeaderId={Survey.LeaderId}");
                _logger.LogInformation($"Questions JSON: {QuestionsJson}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToList()
                        );

                    _logger.LogWarning($"Validation failed: {JsonSerializer.Serialize(errors)}");

                    // Create a more user-friendly error message
                    var errorMessages = errors.Values.SelectMany(e => e).ToList();
                    TempData["ErrorMessage"] = $"Please fix the following errors: {string.Join(", ", errorMessages)}";

                    // Load the leaders for the form
                    Leaders = await _context.Leaders.OrderBy(l => l.Name).ToListAsync();
                    return Page();
                }

                // Get the existing survey
                var survey = await _context.Surveys
                    .Include(s => s.Questions)
                    .FirstOrDefaultAsync(s => s.Id == Survey.Id);

                if (survey == null)
                {
                    return NotFound();
                }

                // Update the survey properties
                survey.Name = Survey.Name.Trim();
                survey.Description = Survey.Description?.Trim() ?? string.Empty;
                survey.Area = Survey.Area;
                survey.LeaderId = Survey.LeaderId;
                survey.EvaluatorLeaderId = Survey.EvaluatorLeaderId;
                survey.MonthYear = Survey.MonthYear.HasValue
                    ? DateTime.SpecifyKind(Survey.MonthYear.Value, DateTimeKind.Utc)
                    : null;

                // Parse the JSON data
                List<QuestionModel> questionsFromJson = new List<QuestionModel>();
                if (!string.IsNullOrEmpty(QuestionsJson))
                {
                    try
                    {
                        // Deserialize the JSON data directly to a list of QuestionModel objects
                        questionsFromJson = JsonSerializer.Deserialize<List<QuestionModel>>(QuestionsJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (questionsFromJson != null)
                        {
                            _logger.LogInformation($"Successfully parsed {questionsFromJson.Count} questions from JSON");
                            foreach (var q in questionsFromJson)
                            {
                                _logger.LogInformation($"Parsed question - ID: {q.Id}, Text: {q.Text}, Type: {q.QuestionType}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Deserialized questions is null");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing questions JSON");
                    }
                }
                else
                {
                    _logger.LogWarning("QuestionsJson is null or empty");
                }

                // Log the existing questions in the database
                _logger.LogInformation($"Survey has {survey.Questions?.Count ?? 0} existing questions in the database");
                foreach (var q in survey.Questions ?? Enumerable.Empty<Question>())
                {
                    _logger.LogInformation($"Existing Question ID: {q.Id}, Text: {q.Text}, Type: {q.QuestionType}");
                }

                // Create a dictionary of existing questions by ID for easier lookup
                var existingQuestions = survey.Questions?.ToDictionary(q => q.Id) ?? new Dictionary<int, Question>();

                // Track which questions to keep
                var questionIdsToKeep = new HashSet<int>();

                // Update existing questions and add new ones
                if (questionsFromJson != null && questionsFromJson.Any())
                {
                    for (int i = 0; i < questionsFromJson.Count; i++)
                    {
                        var q = questionsFromJson[i];
                        var questionOrder = i + 1;

                        // Check if this is an existing question (Id > 0) and if it exists in our database
                        Question existingQuestion = null;
                        bool isExistingQuestion = q.Id > 0 && existingQuestions.TryGetValue(q.Id, out existingQuestion);

                        if (isExistingQuestion && existingQuestion != null)
                        {
                            // Update existing question
                            _logger.LogInformation($"Updating existing question ID: {q.Id}");
                            existingQuestion.Text = q.Text.Trim();
                            existingQuestion.QuestionType = q.QuestionType;
                            existingQuestion.QuestionOrder = questionOrder;
                            questionIdsToKeep.Add(existingQuestion.Id);
                        }
                        else
                        {
                            // Add new question
                            _logger.LogInformation($"Adding new question: {q.Text} (Type: {q.QuestionType})");
                            var newQuestion = new Question
                            {
                                SurveyId = survey.Id,
                                Text = q.Text.Trim(),
                                QuestionType = q.QuestionType,
                                QuestionOrder = questionOrder
                            };

                            // Explicitly add the new question to the context
                            _context.Questions.Add(newQuestion);
                            _logger.LogInformation($"Added new question to context: {newQuestion.Text}");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No questions received in the form submission");
                }

                // Remove questions that are no longer in the list
                if (survey.Questions != null && survey.Questions.Any())
                {
                    _logger.LogInformation($"Checking for questions to remove. Questions to keep: {string.Join(", ", questionIdsToKeep)}");

                    var questionsToRemove = survey.Questions
                        .Where(q => !questionIdsToKeep.Contains(q.Id))
                        .ToList();

                    _logger.LogInformation($"Found {questionsToRemove.Count} questions to remove");

                    foreach (var question in questionsToRemove)
                    {
                        _logger.LogInformation($"Removing question ID: {question.Id}, Text: {question.Text}");
                        _context.Questions.Remove(question);
                    }
                }
                else
                {
                    _logger.LogInformation("No existing questions to check for removal");
                }

                // Log the state before saving
                _logger.LogInformation("State before saving:");
                _logger.LogInformation($"Survey: ID={survey.Id}, Name={survey.Name}");
                _logger.LogInformation($"Questions to keep: {string.Join(", ", questionIdsToKeep)}");
                _logger.LogInformation($"Questions in context: {_context.Questions.Local.Count}");

                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    _logger.LogInformation($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
                }

                // Save changes to the database
                _logger.LogInformation("Saving changes to the database");
                try
                {
                    var result = await _context.SaveChangesAsync();
                    _logger.LogInformation($"SaveChangesAsync returned: {result}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving changes to the database");
                    throw; // Re-throw to handle it in the outer catch block
                }

                // Verify the questions were saved correctly
                var savedQuestions = await _context.Questions
                    .Where(q => q.SurveyId == survey.Id)
                    .OrderBy(q => q.QuestionOrder)
                    .ToListAsync();

                _logger.LogInformation($"After save: Found {savedQuestions.Count} questions in the database for survey ID {survey.Id}");
                foreach (var q in savedQuestions)
                {
                    _logger.LogInformation($"Saved question - ID: {q.Id}, SurveyId: {q.SurveyId}, Text: {q.Text}, Type: {q.QuestionType}");
                }

                // Add success message
                TempData["SuccessMessage"] = "Survey updated successfully";

                // Redirect back to the edit page to show the updated survey
                if (Request.Query.ContainsKey("returnToEdit") && Request.Query["returnToEdit"] == "true")
                {
                    return RedirectToPage("/EditSurvey", new { id = survey.Id });
                }
                else
                {
                    return RedirectToPage("/Surveys");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating survey");
                TempData["ErrorMessage"] = $"Error updating survey: {ex.Message}";
                return RedirectToPage("/Surveys");
            }
        }
    }
}
