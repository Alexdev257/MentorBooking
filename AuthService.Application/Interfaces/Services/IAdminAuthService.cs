using AuthService.Application.DTOs.Request.Admin;
using AuthService.Application.DTOs.Response.Admin;
using Shared.Contracts.Common.Wrappers;

namespace AuthService.Application.Interfaces.Services;

public interface IAdminAuthService
{
    Task<CommonResponse<TeacherResponseDto>> RegisterTeacherAsync(RegisterTeacherByAdminRequest request, Guid adminId, CancellationToken cancellationToken = default);
    Task<CommonResponse<StudentResponseDto>> RegisterStudentAsync(RegisterStudentByAdminRequest request, Guid adminId, CancellationToken cancellationToken = default);
}
