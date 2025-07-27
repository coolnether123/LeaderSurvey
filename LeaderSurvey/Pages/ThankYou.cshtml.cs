using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeaderSurvey.Pages
{
    public class ThankYouModel : PageModel
    {
        public string? Message { get; set; }

        public void OnGet(string? message)
        {
            Message = message;
        }
    }
}
