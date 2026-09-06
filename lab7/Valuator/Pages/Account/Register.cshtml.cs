using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Services;
using Shared;

namespace Valuator.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserService _userService;
    
    public RegisterModel(UserService userService)
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
        
        User user = new User
        {
            Login = Login,
            PasswordHash = Password
        };
        
        bool success = await _userService.CreateUserAsync(user);
        
        if (!success)
        {
            ModelState.AddModelError(string.Empty, "User with this login already exists");
            return Page();
        }
        
        return RedirectToPage("/Account/Login");
    }
}