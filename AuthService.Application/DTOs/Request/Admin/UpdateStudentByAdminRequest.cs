using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Request.Admin;

public class UpdateStudentByAdminRequest
{
    [Required(ErrorMessage = "FullName is required")]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500)]
    public IFormFile Avatar { get; set; }

    [MaxLength(50)]
    public string? StudentCode { get; set; }

    public bool IsActive { get; set; } = true;
}
