using AuthService.Application.DTOs.Request.Admin;
using AuthService.Application.DTOs.Response.Admin;
using Shared.Contracts.Common.Wrappers;
using SharedContracts.Common.Wrappers.Requests;

namespace AuthService.Application.Interfaces.Services;

public interface IAdminAuthService
{
    Task<CommonResponse<TeacherResponseDto>> RegisterTeacherAsync(RegisterTeacherByAdminRequest request, Guid adminId, CancellationToken cancellationToken = default);
    Task<CommonResponse<StudentResponseDto>> RegisterStudentAsync(RegisterStudentByAdminRequest request, Guid adminId, CancellationToken cancellationToken = default);

    Task<CommonResponse<PaginationResponse<StudentResponseDto>>> GetAllStudentsAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<CommonResponse<StudentResponseDto?>> GetStudentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CommonResponse<StudentResponseDto>> UpdateStudentAsync(Guid id, UpdateStudentByAdminRequest request, CancellationToken cancellationToken = default);
    Task<CommonResponse<StudentResponseDto>> UpdateStudentStatusAsync(Guid id, UpdateStudentStatusRequest request, CancellationToken cancellationToken = default);
    Task<CommonResponse<bool>> DeleteStudentAsync(Guid id, CancellationToken cancellationToken = default);
}
