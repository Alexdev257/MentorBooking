namespace AuthService.Application.DTOs.Response.Admin;

public class StudentResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? StudentCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
