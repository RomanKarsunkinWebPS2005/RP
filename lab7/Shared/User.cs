using System.ComponentModel.DataAnnotations;

namespace Shared;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Login { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string PasswordHash { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}