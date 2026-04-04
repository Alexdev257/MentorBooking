using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Request.Auth;

public class RegisterRequest
{
    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}
