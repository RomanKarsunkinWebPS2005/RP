using System.ComponentModel.DataAnnotations;

namespace Valuator.Models;

public class LoginViewModel
{
    [Required]
    [StringLength(50)]
    public string Login { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;
}