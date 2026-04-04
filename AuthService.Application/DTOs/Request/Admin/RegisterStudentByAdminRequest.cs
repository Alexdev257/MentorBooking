using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Application.DTOs.Request.Admin;

public class RegisterStudentByAdminRequest
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

    /// <summary>Ảnh đại diện — multipart field <c>Avatar</c>.</summary>
    [Required(ErrorMessage = "Avatar image is required")]
    [FromForm(Name = "Avatar")]
    public IFormFile Avatar { get; set; } = null!;

    [MaxLength(50)]
    public string? StudentCode { get; set; }
}
