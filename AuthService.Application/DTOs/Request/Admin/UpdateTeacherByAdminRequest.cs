using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Application.DTOs.Request.Admin;

public class UpdateTeacherByAdminRequest
{
    [Required(ErrorMessage = "FullName is required")]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Ảnh mới (tùy chọn). Bỏ trống để giữ ảnh hiện tại.</summary>
    [FromForm(Name = "Avatar")]
    public IFormFile? Avatar { get; set; }

    [MaxLength(50)]
    public string? Department { get; set; }

    public string? Specialization { get; set; }

    public bool IsActive { get; set; } = true;
}
