using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LeaderSurvey.Data;
using LeaderSurvey.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace LeaderSurvey.Pages
{
    [Authorize]
    public class CategoriesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CategoriesModel(ApplicationDbContext context)
        {
            _context = context;
            Categories = new List<QuestionCategory>();
            CategoryQuestionCounts = new Dictionary<int, int>();
        }

        public List<QuestionCategory> Categories { get; set; }
        public Dictionary<int, int> CategoryQuestionCounts { get; set; }

        public async Task OnGetAsync()
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
}
