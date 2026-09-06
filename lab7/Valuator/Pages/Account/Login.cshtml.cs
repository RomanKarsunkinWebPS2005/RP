using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Services;

namespace Valuator.Pages.Account;

public class LoginModel : PageModel
{
    private readonly UserService _userService;
    
    public LoginModel(UserService userService)
    {
        _userService = userService;
    }
    
    [BindProperty]
    public string Login { get; set; } = string.Empty;
    
    [BindProperty]
    public string Password { get; set; } = string.Empty;
    
    public void OnGet()
    {
    }
    
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Password))
        {
            ModelState.AddModelError(string.Empty, "Login and password are required");
            return Page();
        }
        
        bool isValid = await _userService.ValidateUserAsync(Login, Password);
        Console.WriteLine(isValid);
        
        if (isValid)
        {
            List<Claim> claims =
            [
                new(ClaimTypes.NameIdentifier, Login),
                new(ClaimTypes.Name, Login),
                new(ClaimTypes.Role, "User")
            ];
            
            ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddHours(24)
            });
            
            return RedirectToPage("/Index");
        }
        
        ModelState.AddModelError(string.Empty, "Invalid login or password");
        return Page();
    }
}