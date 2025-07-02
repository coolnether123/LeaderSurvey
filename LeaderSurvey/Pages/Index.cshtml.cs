using LeaderSurvey.Data;
using LeaderSurvey.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace LeaderSurvey.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
            DatabaseConnected = true; // Default to true, will be checked in OnGet
            RecentSurveys = new List<Survey>(); // Initialize the list
        }

        public bool DatabaseConnected { get; set; }
        public List<Survey> RecentSurveys { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Check if DB is connected
                DatabaseConnected = await _context.Database.CanConnectAsync();
                if (DatabaseConnected)
                {
                    RecentSurveys = await _context.Surveys
                        .Include(s => s.Leader)
                        .OrderByDescending(s => s.MonthYear)
                        .Take(5)
                        .ToListAsync();
                }
            }
            catch (Exception)
            {
                DatabaseConnected = false;
                RecentSurveys = new List<Survey>();
            }
        }
    }
}
