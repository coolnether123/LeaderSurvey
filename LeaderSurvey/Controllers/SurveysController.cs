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

        // DELETE: api/Surveys/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSurvey(int id)
        {
            var survey = await _context.Surveys
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
            {
                return NotFound();
            }

            // Delete related survey responses and answers
            var surveyResponses = await _context.SurveyResponses
                .Include(sr => sr.Answers)
                .Where(sr => sr.SurveyId == id)
                .ToListAsync();

            foreach (var response in surveyResponses)
            {
                _context.Answers.RemoveRange(response.Answers);
                _context.SurveyResponses.Remove(response);
            }

            // Delete questions
            _context.Questions.RemoveRange(survey.Questions);

            // Delete the survey
            _context.Surveys.Remove(survey);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}