// Pages/Leaders.cshtml.cs
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;


namespace LeaderSurvey.Pages
{
    public class LeadersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LeadersModel(ApplicationDbContext context)
        {
            _context = context;
            Leaders = new List<Leader>();
            NewLeader = new InputModel { Name = "", Area = "" };
        }

        public required List<Leader> Leaders { get; set; }
        
        [BindProperty]
        public required InputModel NewLeader { get; set; }

        [BindProperty]
        public Leader SelectedLeader { get; set; } = default!;

        public class InputModel
        {
            [Required(ErrorMessage = "Leader name is required")]
            [Display(Name = "Full Name")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
            [RegularExpression(@"^[a-zA-Z\s-']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens and apostrophes")]
            public string Name { get; set; } = string.Empty;
            
            [Required(ErrorMessage = "Work area is required")]
            [Display(Name = "Work Area")]
            public string Area { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            Leaders = await _context.Leaders.OrderBy(l => l.Name).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync([FromBody] JsonElement body)
        {
            try 
            {
                // Extract the values from the JSON body
                var newLeader = body.GetProperty("newLeader");
                var name = newLeader.GetProperty("name").GetString()?.Trim();
                var area = newLeader.GetProperty("area").GetString()?.Trim();

                // Clear existing ModelState errors
                ModelState.Clear();

                // Validate the input
                if (string.IsNullOrWhiteSpace(name))
                {
                    ModelState.AddModelError("NewLeader.Name", "Leader name is required");
                }
                if (string.IsNullOrWhiteSpace(area))
                {
                    ModelState.AddModelError("NewLeader.Area", "Work area is required");
                }

                // Check if the model is valid
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return new JsonResult(new { 
                        success = false, 
                        message = errors 
                    });
                }

                // Check for existing leader
                var existingLeader = await _context.Leaders
                    .FirstOrDefaultAsync(l => l.Name.ToLower() == name.ToLower());
                
                if (existingLeader != null)
                {
                    return new JsonResult(new { 
                        success = false, 
                        message = "A leader with this name already exists" 
                    });
                }

                // Create and save the new leader
                var leader = new Leader 
                { 
                    Name = name,
                    Area = area
                };

                _context.Leaders.Add(leader);
                await _context.SaveChangesAsync();

                return new JsonResult(new { 
                    success = true, 
                    id = leader.Id,
                    name = leader.Name,
                    area = leader.Area
                });
            }
            catch (Exception ex) 
            {
                return new JsonResult(new { 
                    success = false, 
                    message = $"Failed to save leader: {ex.Message}" 
                });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var leader = await _context.Leaders.FindAsync(id);
            if (leader != null)
            {
                _context.Leaders.Remove(leader);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Leaders");
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var leaderToUpdate = await _context.Leaders.FindAsync(SelectedLeader.Id);
            if (leaderToUpdate == null)
            {
                return NotFound();
            }

            leaderToUpdate.Name = SelectedLeader.Name;
            leaderToUpdate.Area = SelectedLeader.Area;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Leaders.Any(l => l.Id == SelectedLeader.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Leaders");
        }
    }
}
