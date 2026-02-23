using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Request.Admin;

public class RegisterTeacherByAdminRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "FullName is required")]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [MaxLength(255)]
    public string? Department { get; set; }

    [MaxLength(255)]
    public string? Specialization { get; set; }
}
