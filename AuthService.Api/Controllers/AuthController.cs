using AuthService.Application.DTOs.Request.Auth;
using AuthService.Application.DTOs.Response.Auth;
using AuthService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shared.Contracts.Common.Wrappers;

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

    private static void FillValidationErrors<T>(CommonResponse<T> response, ModelStateDictionary modelState)
    {
        response.IsSuccess = false;
        response.Message = "Validation failed";
        foreach (var key in modelState.Keys)
            foreach (var err in modelState[key]!.Errors)
                response.ListErrors.Add(new Errors { Field = key, Detail = err.ErrorMessage });
    }

    [HttpPost("login")]
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
}
