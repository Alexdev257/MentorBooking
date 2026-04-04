using AuthService.Application.DTOs.Response.Admin;
using AuthService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Common.Wrappers;

namespace AuthService.Api.Controllers;

/// <summary>Internal endpoint for service-to-service user lookup.</summary>
[Route("api/users")]
[ApiController]
[AllowAnonymous]
public class UsersController : ControllerBase
{
    private readonly IAdminAuthService _adminAuthService;

    public UsersController(IAdminAuthService adminAuthService)
    {
        _adminAuthService = adminAuthService;
    }

    /// <summary>Get basic user info (email, fullName) by User ID. Used internally by other services.</summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(CommonResponse<UserInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserInfoById(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _adminAuthService.GetUserInfoByIdAsync(userId, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
}
