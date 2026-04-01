using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmailService.Api.Controllers;

[ApiController]
[Route("api/email/hello")]
public class HelloController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new { service = "email-service", message = "hello world" });
}

