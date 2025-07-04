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
            Categories = new List<QuestionCategory>();
            CategoryQuestionCounts = new Dictionary<int, int>();
        }

        public required List<Leader> Leaders { get; set; }

        [BindProperty]
        public required InputModel NewLeader { get; set; }

        [BindProperty]
        public Leader SelectedLeader { get; set; } = default!;

        public List<QuestionCategory> Categories { get; set; }
        public Dictionary<int, int> CategoryQuestionCounts { get; set; }

        // Active tab ("leaders" or "categories")
        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "leaders";

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

        public async Task OnGetAsync(string? tab = null)
        {
            // Set active tab if provided
            if (!string.IsNullOrEmpty(tab))
            {
                ActiveTab = tab.ToLower();
            }

            // Load leaders
            Leaders = await _context.Leaders.OrderBy(l => l.Name).ToListAsync();

            // Load categories if the categories tab is active or might be needed
            if (ActiveTab == "categories" || string.IsNullOrEmpty(ActiveTab))
            {
                // Load categories
                Categories = await _context.QuestionCategories
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                // Count questions per category
                var categoryMappings = await _context.QuestionCategoryMappings
                    .GroupBy(qcm => qcm.CategoryId)
                    .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Create a dictionary of category ID to question count
                CategoryQuestionCounts = categoryMappings.ToDictionary(
                    x => x.CategoryId,
                    x => x.Count
                );
            }
        }

        public async Task<IActionResult> OnPostAsync([FromBody] JsonElement body)
        {
            try
            {
                // Extract the values from the JSON body
                var leaderData = body.GetProperty("leader");  // Changed from "newLeader" to "leader"
                var name = leaderData.GetProperty("name").GetString()?.Trim();
                var area = leaderData.GetProperty("area").GetString()?.Trim();

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

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] JsonElement body)
        {
            try
            {
                var id = body.GetProperty("id").GetInt32();
                var leader = await _context.Leaders.FindAsync(id);

                if (leader == null)
                {
                    return new JsonResult(new {
                        success = false,
                        message = "Leader not found"
                    });
                }

                // Optional: Check if leader has any associated surveys
                var hasAssociatedSurveys = await _context.Surveys.AnyAsync(s => s.LeaderId == id);
                if (hasAssociatedSurveys)
                {
                    return new JsonResult(new {
                        success = false,
                        message = "Cannot delete leader with associated surveys"
                    });
                }

                _context.Leaders.Remove(leader);
                await _context.SaveChangesAsync();

                return new JsonResult(new {
                    success = true,
                    message = "Leader deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new {
                    success = false,
                    message = $"Error deleting leader: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync([FromBody] JsonElement body)
        {
            try
            {
                var leaderData = body.GetProperty("leader");
                var id = leaderData.GetProperty("id").GetInt32();
                var name = leaderData.GetProperty("name").GetString()?.Trim();
                var area = leaderData.GetProperty("area").GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(area))
                {
                    return new JsonResult(new {
                        success = false,
                        message = "Name and Area are required"
                    });
                }

                var leaderToUpdate = await _context.Leaders.FindAsync(id);
                if (leaderToUpdate == null)
                {
                    return new JsonResult(new {
                        success = false,
                        message = "Leader not found"
                    });
                }

                // Check if the new name conflicts with another leader (excluding the current leader)
                var existingLeader = await _context.Leaders
                    .FirstOrDefaultAsync(l => l.Id != id && l.Name.ToLower() == name.ToLower());

                if (existingLeader != null)
                {
                    return new JsonResult(new {
                        success = false,
                        message = "A leader with this name already exists"
                    });
                }

                leaderToUpdate.Name = name;
                leaderToUpdate.Area = area;

                await _context.SaveChangesAsync();

                return new JsonResult(new {
                    success = true,
                    id = leaderToUpdate.Id,
                    name = leaderToUpdate.Name,
                    area = leaderToUpdate.Area
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new {
                    success = false,
                    message = $"Failed to update leader: {ex.Message}"
                });
            }
        }
    }
}
