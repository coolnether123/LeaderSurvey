using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using LeaderSurvey.Models;
using Microsoft.AspNetCore.Authorization;

namespace LeaderSurvey.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            _logger.LogInformation("Login page accessed. ReturnUrl: {ReturnUrl}", returnUrl);

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                _logger.LogWarning("Login page loaded with error message: {ErrorMessage}", ErrorMessage);
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;

            // Debug: Check if any users exist
            var userCount = _userManager.Users.Count();
            _logger.LogInformation("Total users in database: {UserCount}", userCount);

            // Debug: List all users (be careful in production!)
            var users = _userManager.Users.Take(5).ToList();
            foreach (var user in users)
            {
                _logger.LogInformation("User found: Email={Email}, UserName={UserName}, EmailConfirmed={EmailConfirmed}",
                    user.Email, user.UserName, user.EmailConfirmed);
            }
        }

        [Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            _logger.LogInformation("=== LOGIN POST REQUEST STARTED ===");
            _logger.LogInformation("Request Method: {Method}", HttpContext.Request.Method);
            _logger.LogInformation("Request Path: {Path}", HttpContext.Request.Path);
            _logger.LogInformation("Request Query: {Query}", HttpContext.Request.QueryString);
            _logger.LogInformation("Content Type: {ContentType}", HttpContext.Request.ContentType);
            _logger.LogInformation("Has Form: {HasForm}", HttpContext.Request.HasFormContentType);
            _logger.LogInformation("Input object is null: {IsNull}", Input == null);
            _logger.LogInformation("Login attempt started for email: {Email}", Input?.Email);

            // Log all form data
            if (HttpContext.Request.HasFormContentType)
            {
                try
                {
                    var form = await HttpContext.Request.ReadFormAsync();
                    _logger.LogInformation("Form keys: {Keys}", string.Join(", ", form.Keys));
                    foreach (var key in form.Keys)
                    {
                        if (key.ToLower().Contains("password"))
                        {
                            _logger.LogInformation("Form[{Key}]: [REDACTED]", key);
                        }
                        else
                        {
                            _logger.LogInformation("Form[{Key}]: {Value}", key, form[key]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading form data in OnPostAsync");
                }
            }

            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid. Attempting login for: {Email}", Input.Email);

                // Debug: Check if user exists
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found with email: {Email}", Input.Email);

                    // Try finding by username instead
                    user = await _userManager.FindByNameAsync(Input.Email);
                    if (user == null)
                    {
                        _logger.LogWarning("User not found with username either: {Email}", Input.Email);
                        ModelState.AddModelError(string.Empty, "User not found. Please check your email address.");
                        return Page();
                    }
                    else
                    {
                        _logger.LogInformation("User found by username: {UserName}, Email: {Email}", user.UserName, user.Email);
                    }
                }
                else
                {
                    _logger.LogInformation("User found by email: {UserName}, Email: {Email}, EmailConfirmed: {EmailConfirmed}",
                        user.UserName, user.Email, user.EmailConfirmed);
                }

                // Debug: Check password
                var passwordCheck = await _userManager.CheckPasswordAsync(user, Input.Password);
                _logger.LogInformation("Password check result: {PasswordValid}", passwordCheck);

                _logger.LogInformation("User EmailConfirmed status before sign-in: {EmailConfirmed}", user.EmailConfirmed);

                // Attempt sign in
                var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                _logger.LogInformation("Sign in result - Succeeded: {Succeeded}, IsLockedOut: {IsLockedOut}, IsNotAllowed: {IsNotAllowed}, RequiresTwoFactor: {RequiresTwoFactor}",
                    result.Succeeded, result.IsLockedOut, result.IsNotAllowed, result.RequiresTwoFactor);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Login successful for user: {Email}. Redirecting to: {ReturnUrl}", Input.Email, returnUrl);
                    return RedirectToPage("/Surveys");
                }
                else
                {
                    string errorMessage = "Invalid login attempt.";
                    if (result.IsLockedOut)
                    {
                        errorMessage = "Account is locked out.";
                        _logger.LogWarning("Account locked out for user: {Email}", Input.Email);
                    }
                    else if (result.IsNotAllowed)
                    {
                        errorMessage = "Login not allowed. Please confirm your email.";
                        _logger.LogWarning("Login not allowed for user: {Email}", Input.Email);
                    }
                    else if (result.RequiresTwoFactor)
                    {
                        errorMessage = "Two-factor authentication required.";
                        _logger.LogInformation("Two-factor authentication required for user: {Email}", Input.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Login failed for user: {Email}. Reason unknown.", Input.Email);
                    }

                    ModelState.AddModelError(string.Empty, errorMessage);
                    return Page();
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
