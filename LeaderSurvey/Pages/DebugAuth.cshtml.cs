using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using LeaderSurvey.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaderSurvey.Pages
{
    public class DebugAuthModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public DebugAuthModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public string AdminEmail { get; set; }
        public int PasswordLength { get; set; }
        public int TotalUsers { get; set; }
        public bool AdminUserExists { get; set; }
        public bool AdminRoleExists { get; set; }
        public ApplicationUser AdminUser { get; set; }
        public bool IsInAdminRole { get; set; }
        public bool PasswordValid { get; set; }
        public List<ApplicationUser> AllUsers { get; set; }

        public async Task OnGetAsync()
        {
            AdminEmail = _configuration["AdminUser:Email"];
            var password = _configuration["AdminUser:Password"];
            PasswordLength = password?.Length ?? 0;

            TotalUsers = _userManager.Users.Count();
            AdminRoleExists = await _roleManager.RoleExistsAsync("Admin");

            AdminUser = await _userManager.FindByEmailAsync(AdminEmail);
            AdminUserExists = AdminUser != null;

            if (AdminUser != null)
            {
                IsInAdminRole = await _userManager.IsInRoleAsync(AdminUser, "Admin");
                PasswordValid = await _userManager.CheckPasswordAsync(AdminUser, password);
            }

            AllUsers = _userManager.Users.Take(10).ToList();
        }
    }
}
