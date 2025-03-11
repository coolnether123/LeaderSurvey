// Controllers/LeadersController.cs
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaderSurvey.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeadersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Leaders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Leader>>> GetLeaders()
        {
            return await _context.Leaders.ToListAsync();
        }

        // GET: api/Leaders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Leader>> GetLeader(int id)
        {
            var leader = await _context.Leaders.FindAsync(id);
            if (leader == null)
            {
                return NotFound();
            }
            return leader;
        }

        // POST: api/Leaders
        [HttpPost]
        public async Task<ActionResult<Leader>> PostLeader(Leader leader)
        {
            _context.Leaders.Add(leader);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetLeader), new { id = leader.Id }, leader);
        }
        //PUT for updating
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLeader(int id, Leader leader)
        {
            if (id != leader.Id)
            {
                return BadRequest();
            }

            _context.Entry(leader).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Leaders.Any(l => l.Id == id))
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
        // DELETE: api/Leaders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLeader(int id)
        {
            var leader = await _context.Leaders.FindAsync(id);
            if (leader == null)
            {
                return NotFound();
            }

            _context.Leaders.Remove(leader);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}