using AuthService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedContracts.Common.Wrappers.Requests;

namespace AuthService.Api.Controllers;

/// <summary>Danh sách mentor công khai cho mentee duyệt / đặt lịch (read-only).</summary>
[Route("api/mentors")]
[ApiController]
[AllowAnonymous]
public class MentorDirectoryController : ControllerBase
{
    private readonly IAdminAuthService _adminAuthService;

    public MentorDirectoryController(IAdminAuthService adminAuthService)
    {
        _adminAuthService = adminAuthService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTeachersAsync([FromQuery] PaginationRequest? request, CancellationToken cancellationToken)
    {
        request ??= new PaginationRequest();
        var result = await _adminAuthService.GetAllTeachersAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTeacherByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _adminAuthService.GetTeacherByIdAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
            return NotFound(result);
        return Ok(result);
    }
}
