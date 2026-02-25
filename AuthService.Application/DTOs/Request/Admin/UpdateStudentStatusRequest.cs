namespace AuthService.Application.DTOs.Request.Admin;

/// <summary>Request to update only the status (IsActive) of a student.</summary>
public class UpdateStudentStatusRequest
{
    public bool IsActive { get; set; }
}
