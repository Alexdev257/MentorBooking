using AuthService.Application.DTOs.Request.Auth;
using AuthService.Application.DTOs.Response.Admin;
using AuthService.Application.DTOs.Response.Auth;
using AuthService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shared.Contracts.Common.Wrappers;
using System.Security.Claims;

namespace AuthService.Api.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    private Guid? GetUserIdFromClaim()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("UserId")?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userId, out var id) ? id : null;
    }

    private static void FillValidationErrors<T>(CommonResponse<T> response, ModelStateDictionary modelState)
    {
        response.IsSuccess = false;
        response.Message = "Validation failed";
        foreach (var key in modelState.Keys)
            foreach (var err in modelState[key]!.Errors)
                response.ListErrors.Add(new Errors { Field = key, Detail = err.ErrorMessage });
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(CommonResponse<LoginResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CommonResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest? request)
    {
        if (request == null)
            return BadRequest(new LoginResponse { IsSuccess = false, Message = "Request body is required" });

        if (!ModelState.IsValid)
        {
            var response = new LoginResponse();
            FillValidationErrors(response, ModelState);
            return BadRequest(response);
        }

        var result = await _authService.LoginAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(CommonResponse<LoginResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CommonResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAsync([FromBody] RefreshRequest? request)
    {
        var userId = GetUserIdFromClaim();
        if(userId == null)
        {
            return BadRequest(new LoginResponse { IsSuccess = false, Message = "User must logged in" });
        }
        if (request == null)
            return BadRequest(new LoginResponse { IsSuccess = false, Message = "Request body is required" });

        if (!ModelState.IsValid)
        {
            var response = new LoginResponse();
            FillValidationErrors(response, ModelState);
            return BadRequest(response);
        }

        var result = await _authService.RefreshAsync(userId.Value, request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(CommonResponse<LogoutResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CommonResponse<LogoutResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LogoutAsync()
    {
        var userId = GetUserIdFromClaim();
        if (userId == null)
        {
            return BadRequest(new LoginResponse { IsSuccess = false, Message = "User must logged in" });
        }

        if (!ModelState.IsValid)
        {
            var response = new LogoutResponse();
            FillValidationErrors(response, ModelState);
            return BadRequest(response);
        }

        var result = await _authService.LogoutAsync(userId.Value);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
