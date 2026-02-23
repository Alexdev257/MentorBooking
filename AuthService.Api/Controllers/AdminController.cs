using AuthService.Application.DTOs.Request.Admin;
using AuthService.Application.DTOs.Response.Admin;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shared.Contracts.Common.Wrappers;
using System.Security.Claims;

namespace AuthService.Api.Controllers;

[Route("api/admin")]
[ApiController]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminAuthService _adminAuthService;

    public AdminController(IAdminAuthService adminAuthService)
    {
        _adminAuthService = adminAuthService;
    }

    private Guid? GetAdminIdFromClaim()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("UserId")?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userId, out var id) ? id : null;
    }

    private bool IsAdmin()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        return roleClaim != null && roleClaim == ((int)RoleNameEnum.Admin).ToString();
    }

    /// <summary>Returns 403/401 if not admin; null when ok.</summary>
    private IActionResult? EnsureAdmin(Guid? adminId)
    {
        if (!IsAdmin())
            return StatusCode(StatusCodes.Status403Forbidden, new CommonResponseBase { IsSuccess = false, Message = "Only admin can perform this action" });
        if (adminId == null)
            return Unauthorized(new CommonResponseBase { IsSuccess = false, Message = "Invalid admin context" });
        return null;
    }

    private static void FillValidationErrors<T>(CommonResponse<T> response, ModelStateDictionary modelState)
    {
        response.IsSuccess = false;
        response.Message = "Validation failed";
        foreach (var key in modelState.Keys)
            foreach (var err in modelState[key]!.Errors)
                response.ListErrors.Add(new Errors { Field = key, Detail = err.ErrorMessage });
    }

    /// <summary>Register a new teacher (Admin only).</summary>
    [HttpPost("register-teacher")]
    [ProducesResponseType(typeof(CommonResponse<TeacherResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CommonResponse<TeacherResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegisterTeacherAsync([FromBody] RegisterTeacherByAdminRequest? request)
    {
        if (request == null)
            return BadRequest(new CommonResponse<TeacherResponseDto> { IsSuccess = false, Message = "Request body is required" });

        if (!ModelState.IsValid)
        {
            var response = new CommonResponse<TeacherResponseDto>();
            FillValidationErrors(response, ModelState);
            return BadRequest(response);
        }

        var adminId = GetAdminIdFromClaim();
        if (EnsureAdmin(adminId) is { } err)
            return err;

        var result = await _adminAuthService.RegisterTeacherAsync(request, adminId!.Value);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result) : BadRequest(result);
    }

    /// <summary>Register a new student (Admin only).</summary>
    [HttpPost("register-student")]
    [ProducesResponseType(typeof(CommonResponse<StudentResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CommonResponse<StudentResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegisterStudentAsync([FromBody] RegisterStudentByAdminRequest? request)
    {
        if (request == null)
            return BadRequest(new CommonResponse<StudentResponseDto> { IsSuccess = false, Message = "Request body is required" });

        if (!ModelState.IsValid)
        {
            var response = new CommonResponse<StudentResponseDto>();
            FillValidationErrors(response, ModelState);
            return BadRequest(response);
        }

        var adminId = GetAdminIdFromClaim();
        if (EnsureAdmin(adminId) is { } err)
            return err;

        var result = await _adminAuthService.RegisterStudentAsync(request, adminId!.Value);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result) : BadRequest(result);
    }
}
