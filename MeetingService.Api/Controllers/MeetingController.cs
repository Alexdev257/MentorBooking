using MediatR;
using MeetingService.Application.CQRS.Command;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Common.Wrappers;
using System;
using System.Threading.Tasks;

namespace MeetingService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeetingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MeetingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id}/join")]
        [Authorize(Roles = "1,2,3")] // Allows Admin(1), Mentor(2), Mentee(3)
        public async Task<IActionResult> JoinMeeting(Guid id)
        {
            // Extract the UserId from the JWT token (adjust claim type if needed)
            var userIdString = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                // Fallback for testing if authorization is bypassed or user id claim is named differently
                userId = Guid.NewGuid(); 
            }

            var command = new JoinMeetingCommand
            {
                MeetingId = id,
                UserId = userId
            };

            var response = await _mediator.Send(command);

            if (response.IsSuccess)
            {
                // In a real frontend application, returning a URL in the payload
                // allows the frontend to `window.location.href = data`
                return Ok(response);
            }

            return BadRequest(response);
        }
    }
}
