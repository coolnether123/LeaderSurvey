// Controllers/SurveysController.cs
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaderSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SurveysController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SurveysController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Surveys
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Survey>>> GetSurveys()
        {
            return await _context.Surveys.ToListAsync();
        }

        // GET: api/Surveys/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Survey>> GetSurvey(int id)
        {
            var survey = await _context.Surveys.FindAsync(id);

            if (survey == null)
            {
                return NotFound();
            }

            return survey;
        }

        // POST: api/Surveys
        [HttpPost]
        public async Task<ActionResult<Survey>> PostSurvey(Survey survey)
        {
            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSurvey), new { id = survey.Id }, survey);
        }

        // PUT: api/Surveys/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSurvey(int id, Survey survey)
        {
            if (id != survey.Id)
            {
                return BadRequest();
            }

            _context.Entry(survey).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SurveyExists(id))
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

        // DELETE: api/Surveys/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSurvey(int id)
        {
            var survey = await _context.Surveys.FindAsync(id);
            if (survey == null)
            {
                return NotFound();
            }

            _context.Surveys.Remove(survey);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SurveyExists(int id)
        {
            return _context.Surveys.Any(e => e.Id == id);
        }
    }
}