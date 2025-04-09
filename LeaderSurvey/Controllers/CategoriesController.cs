using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaderSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuestionCategory>>> GetCategories()
        {
            return await _context.QuestionCategories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<QuestionCategory>> GetCategory(int id)
        {
            var category = await _context.QuestionCategories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return category;
        }

        // GET: api/Categories/5/Questions
        [HttpGet("{id}/Questions")]
        public async Task<ActionResult<IEnumerable<Question>>> GetCategoryQuestions(int id)
        {
            var category = await _context.QuestionCategories
                .Include(c => c.QuestionMappings)
                .ThenInclude(qm => qm.Question)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            var questions = category.QuestionMappings
                .Select(qm => qm.Question)
                .Where(q => q != null)
                .ToList();

            return questions;
        }

        // GET: api/Categories/Statistics
        [HttpGet("Statistics")]
        public async Task<ActionResult<object>> GetCategoryStatistics(
            [FromQuery] int? leaderId = null,
            [FromQuery] string? area = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null)
        {
            // Build the query for surveys based on filters
            var surveysQuery = _context.Surveys
                .Include(s => s.Questions)
                .ThenInclude(q => q.CategoryMappings)
                .ThenInclude(cm => cm.Category)
                .Include(s => s.Questions)
                .ThenInclude(q => q.Survey)
                .Where(s => s.Status == "Completed");

            // Apply filters
            if (leaderId.HasValue)
            {
                surveysQuery = surveysQuery.Where(s => s.LeaderId == leaderId);
            }

            if (!string.IsNullOrEmpty(area))
            {
                surveysQuery = surveysQuery.Where(s => s.Area == area);
            }

            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            {
                surveysQuery = surveysQuery.Where(s => s.MonthYear >= start);
            }

            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            {
                // Set to end of day
                end = end.AddDays(1).AddTicks(-1);
                surveysQuery = surveysQuery.Where(s => s.MonthYear <= end);
            }

            // Execute the query
            var surveys = await surveysQuery.ToListAsync();

            // Get all categories
            var categories = await _context.QuestionCategories.ToListAsync();

            // Get all answers for the filtered surveys
            var surveyIds = surveys.Select(s => s.Id).ToList();
            var answers = await _context.Answers
                .Include(a => a.Question)
                .Include(a => a.SurveyResponse)
                .Where(a => a.SurveyResponse != null && surveyIds.Contains(a.SurveyResponse.SurveyId))
                .ToListAsync();

            // Calculate statistics for each category
            var statistics = categories.Select(category =>
            {
                // Get all questions in this category from the filtered surveys
                var categoryQuestions = surveys
                    .SelectMany(s => s.Questions)
                    .Where(q => q.CategoryMappings.Any(cm => cm.CategoryId == category.Id))
                    .ToList();

                // Get question IDs for this category
                var questionIds = categoryQuestions.Select(q => q.Id).ToList();

                // Get answers for these questions
                var categoryAnswers = answers
                    .Where(a => questionIds.Contains(a.QuestionId))
                    .ToList();

                // Calculate Yes/No statistics
                var yesNoQuestions = categoryQuestions
                    .Where(q => q.QuestionType == "yesno")
                    .ToList();

                var yesNoAnswers = categoryAnswers
                    .Where(a => a.Question.QuestionType == "yesno")
                    .ToList();

                int yesCount = yesNoAnswers.Count(a => a.Response == "Yes");
                int totalYesNo = yesNoAnswers.Count;
                double yesPercentage = totalYesNo > 0 ? (double)yesCount / totalYesNo * 100 : 0;

                // Calculate Score statistics
                var scoreQuestions = categoryQuestions
                    .Where(q => q.QuestionType == "score")
                    .ToList();

                var scoreAnswers = categoryAnswers
                    .Where(a => a.Question.QuestionType == "score" && int.TryParse(a.Response, out _))
                    .ToList();

                var scoreValues = scoreAnswers
                    .Select(a => int.Parse(a.Response))
                    .ToList();

                double averageScore = scoreValues.Any() ? scoreValues.Average() : 0;

                return new
                {
                    Category = category,
                    YesNoQuestions = yesNoQuestions.Count,
                    YesNoAnswers = totalYesNo,
                    YesPercentage = Math.Round(yesPercentage, 1),
                    ScoreQuestions = scoreQuestions.Count,
                    ScoreAnswers = scoreAnswers.Count,
                    AverageScore = Math.Round(averageScore, 1),
                    Questions = categoryQuestions.Select(q => new
                    {
                        q.Id,
                        q.Text,
                        q.QuestionType,
                        Answers = categoryAnswers
                            .Where(a => a.QuestionId == q.Id)
                            .Select(a => a.Response)
                            .ToList()
                    }).ToList()
                };
            }).ToList();

            return new { Statistics = statistics };
        }

        // POST: api/Categories
        [HttpPost]
        public async Task<ActionResult<QuestionCategory>> CreateCategory(QuestionCategory category)
        {
            _context.QuestionCategories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, QuestionCategory category)
        {
            if (id != category.Id)
            {
                return BadRequest();
            }

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.QuestionCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            // Check if this category is used by any questions
            var hasQuestions = await _context.QuestionCategoryMappings
                .AnyAsync(qcm => qcm.CategoryId == id);

            if (hasQuestions)
            {
                return BadRequest("Cannot delete category because it is used by one or more questions");
            }

            _context.QuestionCategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(int id)
        {
            return _context.QuestionCategories.Any(e => e.Id == id);
        }
    }
}
