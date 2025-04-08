using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeaderSurvey.Data;
using LeaderSurvey.Models;

namespace LeaderSurvey.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SurveysController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SurveysController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Surveys/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetSurvey(int id)
        {
            try
            {
                // First check if the survey exists
                var surveyExists = await _context.Surveys.AnyAsync(s => s.Id == id);
                if (!surveyExists)
                {
                    return NotFound(new { message = $"Survey with ID {id} not found" });
                }

                // Get the survey without includes first
                var survey = await _context.Surveys.FindAsync(id);
                if (survey == null)
                {
                    return NotFound(new { message = $"Survey with ID {id} not found after initial check" });
                }

                // Get questions separately
                var questions = await _context.Questions
                    .Where(q => q.SurveyId == id)
                    .OrderBy(q => q.QuestionOrder)
                    .ToListAsync();

                // Get leader separately
                var leader = survey.LeaderId.HasValue ?
                    await _context.Leaders.FindAsync(survey.LeaderId.Value) : null;

                // Get evaluator leader separately
                var evaluatorLeader = survey.EvaluatorLeaderId.HasValue ?
                    await _context.Leaders.FindAsync(survey.EvaluatorLeaderId.Value) : null;

                // Create a simplified response object
                var result = new
                {
                    survey.Id,
                    survey.Name,
                    survey.Description,
                    survey.Area,
                    survey.LeaderId,
                    Leader = leader != null ? new { leader.Id, leader.Name, leader.Area } : null,
                    survey.EvaluatorLeaderId,
                    EvaluatorLeader = evaluatorLeader != null ? new { evaluatorLeader.Id, evaluatorLeader.Name, evaluatorLeader.Area } : null,
                    survey.MonthYear,
                    survey.Date,
                    survey.Status,
                    Questions = questions.Select(q => new { q.Id, q.Text, q.QuestionType, q.QuestionOrder })
                };

                return result;
            }
            catch (Exception ex)
            {
                // Log the full exception details
                Console.Error.WriteLine($"Error retrieving survey {id}: {ex.Message}");
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                return StatusCode(500, new { message = $"Error retrieving survey: {ex.Message}" });
            }
        }

        // DELETE: api/Surveys/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSurvey(int id)
        {
            try
            {
                // Use a more direct approach to avoid column mismatches
                var survey = await _context.Surveys.FindAsync(id);

                if (survey == null)
                {
                    return NotFound(new { message = "Survey not found" });
                }

                // Get questions directly with a separate query
                var questions = await _context.Questions
                    .Where(q => q.SurveyId == id)
                    .ToListAsync();

                // Get survey responses with a separate query
                var surveyResponseIds = await _context.SurveyResponses
                    .Where(sr => sr.SurveyId == id)
                    .Select(sr => sr.Id)
                    .ToListAsync();

                // Get answers for these responses
                var answers = await _context.Answers
                    .Where(a => surveyResponseIds.Contains(a.SurveyResponseId))
                    .ToListAsync();

                // Delete in the correct order to maintain referential integrity
                _context.Answers.RemoveRange(answers);
                await _context.SaveChangesAsync();

                // Delete survey responses
                var responses = await _context.SurveyResponses
                    .Where(sr => sr.SurveyId == id)
                    .ToListAsync();
                _context.SurveyResponses.RemoveRange(responses);
                await _context.SaveChangesAsync();

                // Delete questions
                _context.Questions.RemoveRange(questions);
                await _context.SaveChangesAsync();

                // Finally delete the survey
                _context.Surveys.Remove(survey);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Survey deleted successfully" });
            }
            catch (Exception ex)
            {
                // For database-specific errors, try a more direct approach
                if (ex.Message.Contains("42703") || ex.Message.Contains("column") || ex.Message.Contains("does not exist"))
                {
                    try
                    {
                        // Try a direct SQL approach as a last resort with parameterized queries
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Questions\" WHERE \"SurveyId\" = {0}", id);
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Answers\" WHERE \"SurveyResponseId\" IN (SELECT \"Id\" FROM \"SurveyResponses\" WHERE \"SurveyId\" = {0})", id);
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"SurveyResponses\" WHERE \"SurveyId\" = {0}", id);
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"Surveys\" WHERE \"Id\" = {0}", id);

                        return Ok(new { message = "Survey deleted successfully using direct SQL" });
                    }
                    catch (Exception innerEx)
                    {
                        return StatusCode(500, new { message = $"Failed to delete survey using direct SQL: {innerEx.Message}" });
                    }
                }

                return StatusCode(500, new { message = $"An error occurred while deleting the survey: {ex.Message}" });
            }
        }
    }
}