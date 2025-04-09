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

        [BindProperty]
        public string returnToEdit { get; set; } = "true";

        public SelectList LeaderSelectList { get; set; } = default!;
        public SelectList AreaSelectList { get; set; } = default!;
        public List<QuestionCategory> Categories { get; set; } = new();

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

            public List<int> CategoryIds { get; set; } = new();
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

                // Load questions separately - use AsNoTracking to ensure we get fresh data
                _logger.LogInformation($"Loading questions for survey ID {id}");
                var questions = await _context.Questions
                    .AsNoTracking() // Ensure we get fresh data from the database
                    .Where(q => q.SurveyId == id)
                    .OrderBy(q => q.QuestionOrder)
                    .ToListAsync();

                _logger.LogInformation($"Found {questions.Count} questions in the database for survey ID {id}");
                foreach (var q in questions)
                {
                    _logger.LogInformation($"Database question - ID: {q.Id}, SurveyId: {q.SurveyId}, Text: {q.Text}, Type: {q.QuestionType}, Order: {q.QuestionOrder}");
                }

                // Clear any existing questions and add the loaded ones
                if (survey.Questions == null)
                {
                    survey.Questions = new List<Question>();
                }
                else
                {
                    survey.Questions.Clear();
                }

                foreach (var q in questions)
                {
                    survey.Questions.Add(q);
                }

                // Double-check that the questions are loaded correctly
                _logger.LogInformation($"Survey has {survey.Questions.Count} questions after assignment");

                // Verify using a direct SQL query
                try
                {
                    var sql = $"SELECT \"Id\", \"SurveyId\", \"Text\", \"QuestionType\", \"QuestionOrder\" FROM \"Questions\" WHERE \"SurveyId\" = {id} ORDER BY \"QuestionOrder\"";
                    _logger.LogInformation($"Executing SQL query on page load: {sql}");

                    var connection = _context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var count = 0;
                            while (await reader.ReadAsync())
                            {
                                count++;
                                var qId = reader.GetInt32(0);
                                var surveyId = reader.GetInt32(1);
                                var text = reader.GetString(2);
                                var type = reader.GetString(3);
                                var order = reader.GetInt32(4);

                                _logger.LogInformation($"SQL query on page load found question - ID: {qId}, SurveyId: {surveyId}, Text: {text}, Type: {type}, Order: {order}");
                            }
                            _logger.LogInformation($"SQL query on page load found {count} questions for survey ID {id}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing SQL query on page load");
                }

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

                // Load categories
                Categories = await _context.QuestionCategories
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                // Load question categories
                var questionCategoryMappings = await _context.QuestionCategoryMappings
                    .Where(qcm => survey.Questions.Select(q => q.Id).Contains(qcm.QuestionId))
                    .ToListAsync();

                // Assign categories to questions
                foreach (var question in Survey.Questions)
                {
                    if (question.CategoryIds == null)
                    {
                        question.CategoryIds = new List<int>();
                    }

                    question.CategoryIds = questionCategoryMappings
                        .Where(qcm => qcm.QuestionId == question.Id)
                        .Select(qcm => qcm.CategoryId)
                        .ToList();
                }

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

                            // Update category mappings for existing question
                            // First, remove all existing mappings
                            var existingMappings = await _context.QuestionCategoryMappings
                                .Where(qcm => qcm.QuestionId == existingQuestion.Id)
                                .ToListAsync();

                            if (existingMappings.Any())
                            {
                                _logger.LogInformation($"Removing {existingMappings.Count} existing category mappings for question ID: {existingQuestion.Id}");
                                _context.QuestionCategoryMappings.RemoveRange(existingMappings);
                            }

                            // Then add new mappings based on the updated categories
                            if (q.CategoryIds != null && q.CategoryIds.Any())
                            {
                                _logger.LogInformation($"Adding {q.CategoryIds.Count} new category mappings for question ID: {existingQuestion.Id}");
                                foreach (var categoryId in q.CategoryIds)
                                {
                                    var mapping = new QuestionCategoryMapping
                                    {
                                        QuestionId = existingQuestion.Id,
                                        CategoryId = categoryId
                                    };
                                    _context.QuestionCategoryMappings.Add(mapping);
                                    _logger.LogInformation($"Added category mapping: QuestionId={existingQuestion.Id}, CategoryId={categoryId}");
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"No categories specified for existing question ID {existingQuestion.Id}");
                            }
                        }
                        else
                        {
                            // Add new question
                            _logger.LogInformation($"Adding new question: {q.Text} (Type: {q.QuestionType})");
                            _logger.LogInformation($"Survey ID for new question: {survey.Id}");

                            var newQuestion = new Question
                            {
                                SurveyId = survey.Id,
                                Text = q.Text.Trim(),
                                QuestionType = q.QuestionType,
                                QuestionOrder = questionOrder
                            };

                            // Explicitly add the new question to the context
                            _logger.LogInformation($"Adding new question to context: SurveyId={newQuestion.SurveyId}, Text={newQuestion.Text}, Type={newQuestion.QuestionType}");

                            // Use a direct SQL command to insert the question
                            var sql = $"INSERT INTO \"Questions\" (\"SurveyId\", \"Text\", \"QuestionType\", \"QuestionOrder\") VALUES ({newQuestion.SurveyId}, '{newQuestion.Text.Replace("'", "''")}', '{newQuestion.QuestionType}', {newQuestion.QuestionOrder}) RETURNING \"Id\"";
                            _logger.LogInformation($"Executing SQL: {sql}");

                            try
                            {
                                var newId = await _context.Database.ExecuteSqlRawAsync(sql);
                                _logger.LogInformation($"SQL insert returned: {newId}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error executing SQL insert");
                            }

                            // Also add to the context as a backup
                            _context.Questions.Add(newQuestion);
                            _logger.LogInformation($"Added new question to context: {newQuestion.Text}");

                            // Save changes to get the new question ID
                            await _context.SaveChangesAsync();

                            // Add category mappings for the new question
                            if (q.CategoryIds != null && q.CategoryIds.Any())
                            {
                                foreach (var categoryId in q.CategoryIds)
                                {
                                    var mapping = new QuestionCategoryMapping
                                    {
                                        QuestionId = newQuestion.Id,
                                        CategoryId = categoryId
                                    };
                                    _context.QuestionCategoryMappings.Add(mapping);
                                    _logger.LogInformation($"Added category mapping: QuestionId={newQuestion.Id}, CategoryId={categoryId}");
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"No categories specified for question ID {newQuestion.Id}");
                            }
                        }
                    }

                    // We'll handle category mappings in a separate step
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

                // Check what's in the database before saving
                _logger.LogInformation("Checking database before saving");
                try
                {
                    var sql = $"SELECT \"Id\", \"SurveyId\", \"Text\", \"QuestionType\", \"QuestionOrder\" FROM \"Questions\" WHERE \"SurveyId\" = {survey.Id} ORDER BY \"QuestionOrder\"";
                    _logger.LogInformation($"Executing SQL query: {sql}");

                    var connection = _context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var count = 0;
                            while (await reader.ReadAsync())
                            {
                                count++;
                                var id = reader.GetInt32(0);
                                var surveyId = reader.GetInt32(1);
                                var text = reader.GetString(2);
                                var type = reader.GetString(3);
                                var order = reader.GetInt32(4);

                                _logger.LogInformation($"Before save: Found question - ID: {id}, SurveyId: {surveyId}, Text: {text}, Type: {type}, Order: {order}");
                            }
                            _logger.LogInformation($"Before save: Found {count} questions for survey ID {survey.Id}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking database before saving");
                }

                // Save changes to the database
                _logger.LogInformation("Saving changes to the database");
                try
                {
                    // Force the context to detach all entities and reload from the database
                    foreach (var entry in _context.ChangeTracker.Entries())
                    {
                        _logger.LogInformation($"Entity before save: {entry.Entity.GetType().Name}, State: {entry.State}");
                    }

                    var result = await _context.SaveChangesAsync();
                    _logger.LogInformation($"SaveChangesAsync returned: {result} changes saved");

                    // Clear the context to ensure we get fresh data
                    _context.ChangeTracker.Clear();

                    // Verify the questions were saved correctly using a direct SQL query
                    var sql = $"SELECT \"Id\", \"SurveyId\", \"Text\", \"QuestionType\", \"QuestionOrder\" FROM \"Questions\" WHERE \"SurveyId\" = {survey.Id} ORDER BY \"QuestionOrder\"";
                    _logger.LogInformation($"Executing SQL query: {sql}");

                    try
                    {
                        var connection = _context.Database.GetDbConnection();
                        if (connection.State != System.Data.ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = sql;
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                var count = 0;
                                while (await reader.ReadAsync())
                                {
                                    count++;
                                    var id = reader.GetInt32(0);
                                    var surveyId = reader.GetInt32(1);
                                    var text = reader.GetString(2);
                                    var type = reader.GetString(3);
                                    var order = reader.GetInt32(4);

                                    _logger.LogInformation($"SQL query found question - ID: {id}, SurveyId: {surveyId}, Text: {text}, Type: {type}, Order: {order}");
                                }
                                _logger.LogInformation($"SQL query found {count} questions for survey ID {survey.Id}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing SQL query");
                    }

                    // Also verify using EF Core
                    var savedQuestions = await _context.Questions
                        .AsNoTracking() // Ensure we get fresh data from the database
                        .Where(q => q.SurveyId == survey.Id)
                        .OrderBy(q => q.QuestionOrder)
                        .ToListAsync();

                    _logger.LogInformation($"After save: Found {savedQuestions.Count} questions in the database for survey ID {survey.Id}");
                    foreach (var q in savedQuestions)
                    {
                        _logger.LogInformation($"Saved question - ID: {q.Id}, SurveyId: {q.SurveyId}, Text: {q.Text}, Type: {q.QuestionType}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving changes to the database");
                    throw; // Re-throw to handle it in the outer catch block
                }

                // Add success message
                TempData["SuccessMessage"] = "Survey updated successfully";

                // Check if we should return to the edit page or go back to the surveys list
                if (returnToEdit.ToLower() == "true")
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
