using AuthService.Application.DTOs.Request.Admin;
using AuthService.Application.DTOs.Response.Admin;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shared.Contracts.Common.Wrappers;
using SharedContracts.Common.Wrappers.Requests;
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

    /// <summary>Get all students with paging (Admin only).</summary>
    [HttpGet("students")]
    [ProducesResponseType(typeof(CommonResponse<PaginationResponse<StudentResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllStudentsAsync([FromQuery] PaginationRequest? request)
    {
        request ??= new PaginationRequest();
        var adminId = GetAdminIdFromClaim();
        if (EnsureAdmin(adminId) is { } err)
            return err;
        var result = await _adminAuthService.GetAllStudentsAsync(request);
        return Ok(result);
    }

    [HttpGet("students/{id:guid}")]
    [ProducesResponseType(typeof(CommonResponse<StudentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudentByIdAsync(Guid id)
    {
        var adminId = GetAdminIdFromClaim();
        if (EnsureAdmin(adminId) is { } err)
            return err;
        var result = await _adminAuthService.GetStudentByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPut("students/{id:guid}")]
    [ProducesResponseType(typeof(CommonResponse<StudentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommonResponse<StudentResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStudentAsync(Guid id, [FromBody] UpdateStudentByAdminRequest? request)
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
        var result = await _adminAuthService.UpdateStudentAsync(id, request);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Update only student status (IsActive). Admin only.</summary>
    [HttpPatch("students/{id:guid}/status")]
    [ProducesResponseType(typeof(CommonResponse<StudentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommonResponse<StudentResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStudentStatusAsync(Guid id, [FromBody] UpdateStudentStatusRequest? request)
    {
        if (request == null)
            return BadRequest(new CommonResponse<StudentResponseDto> { IsSuccess = false, Message = "Request body is required" });
        var adminId = GetAdminIdFromClaim();
        if (EnsureAdmin(adminId) is { } err)
            return err;
        var result = await _adminAuthService.UpdateStudentStatusAsync(id, request);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Soft delete: set student status to inactive (IsActive = false). Admin only.</summary>
    [HttpDelete("students/{id:guid}")]
    [ProducesResponseType(typeof(CommonResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteStudentAsync(Guid id)
    {
        var adminId = GetAdminIdFromClaim();
        if (EnsureAdmin(adminId) is { } err)
            return err;
        var result = await _adminAuthService.DeleteStudentAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }



    /// <summary>Get all teachers with paging (Admin only).</summary>
    [HttpGet("teachers")]
    [ProducesResponseType(typeof(CommonResponse<PaginationResponse<TeacherResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllTeachersAsync([FromQuery] PaginationRequest? request)
    {
        request ??= new PaginationRequest();
        var adminId = GetAdminIdFromClaim();
        if (EnsureAdmin(adminId) is { } err)
            return err;
        var result = await _adminAuthService.GetAllTeachersAsync(request);
        return Ok(result);
    }

    [HttpGet("teachers/{id:guid}")]
    [ProducesResponseType(typeof(CommonResponse<TeacherResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTeacherByIdAsync(Guid id)
    {
        var adminId = GetAdminIdFromClaim();
        if (EnsureAdmin(adminId) is { } err)
            return err;
        var result = await _adminAuthService.GetTeacherByIdAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPut("teachers/{id:guid}")]
    [ProducesResponseType(typeof(CommonResponse<TeacherResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommonResponse<TeacherResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTeacherAsync(Guid id, [FromBody] UpdateTeacherByAdminRequest? request)
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
        var result = await _adminAuthService.UpdateTeacherAsync(id, request);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Update only teacher status (IsActive). Admin only.</summary>
    [HttpPatch("teachers/{id:guid}/status")]
    [ProducesResponseType(typeof(CommonResponse<TeacherResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommonResponse<TeacherResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStudentStatusAsync(Guid id, [FromBody] UpdateTeacherStatusRequest? request)
    {
        if (request == null)
            return BadRequest(new CommonResponse<TeacherResponseDto> { IsSuccess = false, Message = "Request body is required" });
        var adminId = GetAdminIdFromClaim();
        if (EnsureAdmin(adminId) is { } err)
            return err;
        var result = await _adminAuthService.UpdateTeacherStatusAsync(id, request);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Soft delete: set teacher status to inactive (IsActive = false). Admin only.</summary>
    [HttpDelete("teachers/{id:guid}")]
    [ProducesResponseType(typeof(CommonResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTeacherAsync(Guid id)
    {
        var adminId = GetAdminIdFromClaim();
        if (EnsureAdmin(adminId) is { } err)
            return err;
        var result = await _adminAuthService.DeleteTeacherAsync(id);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
}
