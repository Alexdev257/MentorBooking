namespace AuthService.Application.DTOs.Response.Admin;

public class TeacherResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Department { get; set; }
    public string? Specialization { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
