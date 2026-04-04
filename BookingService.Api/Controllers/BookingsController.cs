using BookingService.Application.DTOs.Request;
using BookingService.Application.DTOs.Response;
using BookingService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shared.Contracts.Common.Wrappers;
using SharedContracts.Common.Wrappers.Requests;
using System.Security.Claims;

namespace BookingService.Api.Controllers;

[Route("api/booking")]
[ApiController]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IZoomService _zoomService;

    public BookingsController(IBookingService bookingService, IZoomService zoomService)
    {
        _bookingService = bookingService;
        _zoomService = zoomService;
    }

    private Guid? GetUserIdFromClaim()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("UserId")?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userId, out var id) ? id : null;
    }

    private bool IsMentor()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        return role == "2";
    }

    private static void FillValidationErrors<T>(CommonResponse<T> response, ModelStateDictionary modelState)
    {
        response.IsSuccess = false;
        response.Message = "Validation failed";
        foreach (var key in modelState.Keys)
            foreach (var err in modelState[key]!.Errors)
                response.ListErrors.Add(new Errors { Field = key, Detail = err.ErrorMessage });
    }

    /// <summary>Create a booking (student/mentee books a slot). MenteeId = current user.</summary>
    [HttpPost("bookings")]
    [ProducesResponseType(typeof(CommonResponse<BookingResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CommonResponse<BookingResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest? request)
    {
        var menteeId = GetUserIdFromClaim();
        if (menteeId == null)
            return Unauthorized(new CommonResponse<BookingResponseDto> { IsSuccess = false, Message = "Invalid user context" });
        if (request == null)
            return BadRequest(new CommonResponse<BookingResponseDto> { IsSuccess = false, Message = "Request body is required" });
        if (!ModelState.IsValid)
        {
            var response = new CommonResponse<BookingResponseDto>();
            FillValidationErrors(response, ModelState);
            return BadRequest(response);
        }
        var result = await _bookingService.CreateBookingAsync(menteeId.Value, request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Get booking by id.</summary>
    [HttpGet("bookings/{bookingId:guid}")]
    [ProducesResponseType(typeof(CommonResponse<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBookingById(Guid bookingId)
    {
        var result = await _bookingService.GetBookingByIdAsync(bookingId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Get bookings of the current user as mentee (student).</summary>
    [HttpGet("mentees/{menteeId:guid}/bookings")]
    [ProducesResponseType(typeof(CommonResponse<PaginationResponse<BookingResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMenteeBookings(Guid menteeId, [FromQuery] PaginationRequest? request)
    {
        var userId = GetUserIdFromClaim();
        if (userId == null)
            return Unauthorized(new CommonResponse<PaginationResponse<BookingResponseDto>> { IsSuccess = false, Message = "Invalid user context" });
        if (userId != menteeId)
            return StatusCode(StatusCodes.Status403Forbidden, new CommonResponse<PaginationResponse<BookingResponseDto>> { IsSuccess = false, Message = "You can only view your own bookings" });
        request ??= new PaginationRequest();
        var result = await _bookingService.GetMenteeBookingsAsync(menteeId, request);
        return Ok(result);
    }

    /// <summary>Get bookings of the current user as mentor (teacher). Optional filter by status.</summary>
    [HttpGet("mentors/{mentorId:guid}/bookings")]
    [ProducesResponseType(typeof(CommonResponse<PaginationResponse<BookingResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMentorBookings(Guid mentorId, [FromQuery] PaginationRequest? request, [FromQuery] int? status)
    {
        var userId = GetUserIdFromClaim();
        if (userId == null)
            return Unauthorized(new CommonResponse<PaginationResponse<BookingResponseDto>> { IsSuccess = false, Message = "Invalid user context" });
        if (userId != mentorId)
            return StatusCode(StatusCodes.Status403Forbidden, new CommonResponse<PaginationResponse<BookingResponseDto>> { IsSuccess = false, Message = "You can only view your own mentor bookings" });
        request ??= new PaginationRequest();
        var result = await _bookingService.GetMentorBookingsAsync(mentorId, request, status);
        return Ok(result);
    }

    /// <summary>Mentor accepts a pending booking. Current user must be the mentor. Triggers meeting link flow (to be integrated with MeetingService).</summary>
    [HttpPatch("bookings/{bookingId:guid}/accept")]
    [ProducesResponseType(typeof(CommonResponse<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AcceptBooking(Guid bookingId)
    {
        var mentorId = GetUserIdFromClaim();
        if (mentorId == null)
            return Unauthorized(new CommonResponse<BookingResponseDto> { IsSuccess = false, Message = "Invalid user context" });
        var result = await _bookingService.AcceptBookingAsync(bookingId, mentorId.Value);
        if (!result.IsSuccess)
            return result.Message == "Booking not found" ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>Mentor rejects a pending booking. Current user must be the mentor. Frees the slot.</summary>
    [HttpPatch("bookings/{bookingId:guid}/reject")]
    [ProducesResponseType(typeof(CommonResponse<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectBooking(Guid bookingId)
    {
        var mentorId = GetUserIdFromClaim();
        if (mentorId == null)
            return Unauthorized(new CommonResponse<BookingResponseDto> { IsSuccess = false, Message = "Invalid user context" });
        var result = await _bookingService.RejectBookingAsync(bookingId, mentorId.Value);
        if (!result.IsSuccess)
            return result.Message == "Booking not found" ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>Cancel a booking. Either mentee or mentor can cancel.</summary>
    [HttpPatch("bookings/{bookingId:guid}/cancel")]
    [ProducesResponseType(typeof(CommonResponse<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelBooking(Guid bookingId)
    {
        var userId = GetUserIdFromClaim();
        if (userId == null)
            return Unauthorized(new CommonResponse<BookingResponseDto> { IsSuccess = false, Message = "Invalid user context" });
        var result = await _bookingService.CancelBookingAsync(bookingId, userId.Value, IsMentor());
        if (!result.IsSuccess)
            return result.Message == "Booking not found" ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>Debugging endpoint to get Zoom Access Token.</summary>
    [AllowAnonymous]
    [HttpGet("zoom-token")]
    public async Task<IActionResult> GetZoomAccessToken(CancellationToken cancellationToken)
    {
        try
        {
            var token = await _zoomService.GetAccessToken(cancellationToken);
            return Ok(new { access_token = token });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
