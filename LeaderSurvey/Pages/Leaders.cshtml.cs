// Pages/Leaders.cshtml.cs
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace LeaderSurvey.Pages
{
    public class LeadersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LeadersModel(ApplicationDbContext context)
        {
            _context = context;
            // Initialize collections to empty lists to avoid null reference exceptions
            Leaders = new List<Leader>();
            NewLeader = new InputModel { Name = "", Area = "" };
        }

        // Add required modifier to non-nullable properties
        public required List<Leader> Leaders { get; set; }
        [BindProperty]
        public required InputModel NewLeader { get; set; }
        [BindProperty]
        public Leader SelectedLeader { get; set; } = new Leader { Name = "", Area = "" };
        [TempData]
        public string? StatusMessage { get; set; }  // Make nullable since it's optional

        public class InputModel
        {
            [Required(ErrorMessage = "Leader name is required")]
            [Display(Name = "Full Name")]
            public required string Name { get; set; }
            
            [Required(ErrorMessage = "Work area is required")]
            [Display(Name = "Work Area")]
            public required string Area { get; set; }
        }

        public async Task OnGetAsync(int? id)
        {
            Leaders = await _context.Leaders.ToListAsync();
            
            // Initialize properties to avoid null reference when binding
            NewLeader = new InputModel { Name = "", Area = "" };
            
            if (id.HasValue)
            {
                SelectedLeader = await _context.Leaders.FirstOrDefaultAsync(m => m.Id == id) ?? new Leader { Name = "", Area = "" };
            }
            else
            {
                SelectedLeader = new Leader { Name = "", Area = "" };
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try 
            {
                if (!ModelState.IsValid)
                {
                    Leaders = await _context.Leaders.ToListAsync();
                    StatusMessage = "It's our pleasure to inform you that some required fields need your attention.";
                    return Page();
                }

                var leader = new Leader 
                { 
                    Name = NewLeader.Name.Trim(),
                    Area = NewLeader.Area.Trim()
                };

                _context.Leaders.Add(leader);
                await _context.SaveChangesAsync();
                
                StatusMessage = "It's our pleasure to confirm that the leader was successfully added!";
                return RedirectToPage();
            }
            catch (Exception) // Remove the 'ex' variable since it's not being used
            {
                Leaders = await _context.Leaders.ToListAsync();
                StatusMessage = "We apologize, but we encountered an unexpected error. Please try again.";
                return Page();
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
                // If model state is invalid, return to the page with validation errors
                return Page();
            }

            _context.Attach(SelectedLeader).State = EntityState.Modified;

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
