using BookingService.Application.DTOs.Request;
using BookingService.Application.DTOs.Response;
using BookingService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Common.Wrappers;
using System.Security.Claims;

namespace BookingService.Api.Controllers;

[Route("api/booking/mentors/{mentorId:guid}/slots")]
[ApiController]
[Authorize]
public class SlotsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public SlotsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    private Guid? GetUserIdFromClaim()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("UserId")?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userId, out var id) ? id : null;
    }

    /// <summary>Get available (unbooked) slots of a mentor. Optional filter by date range. Anyone authenticated can view.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CommonResponse<List<SlotResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSlots(Guid mentorId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _bookingService.GetAvailableSlotsAsync(mentorId, from, to);
        return Ok(result);
    }

    /// <summary>Get one slot by id. Mentor only for their own slots.</summary>
    [HttpGet("{slotId:guid}")]
    [ProducesResponseType(typeof(CommonResponse<SlotResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSlotById(Guid mentorId, Guid slotId)
    {
        var result = await _bookingService.GetSlotByIdAsync(mentorId, slotId);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Create a slot. Mentor only (mentorId must match current user).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CommonResponse<SlotResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CommonResponse<SlotResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSlot(Guid mentorId, [FromBody] CreateSlotRequest request)
    {
        var userId = GetUserIdFromClaim();
        if (userId == null)
            return Unauthorized(new CommonResponse<SlotResponseDto> { IsSuccess = false, Message = "Invalid user context" });
        if (userId != mentorId)
            return StatusCode(StatusCodes.Status403Forbidden, new CommonResponse<SlotResponseDto> { IsSuccess = false, Message = "You can only create slots for yourself" });
        if (request == null)
            return BadRequest(new CommonResponse<SlotResponseDto> { IsSuccess = false, Message = "Request body is required" });
        var result = await _bookingService.CreateSlotAsync(mentorId, request);
        if (!result.IsSuccess)
            return BadRequest(result);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Update a slot. Mentor only. Cannot update booked slots.</summary>
    [HttpPut("{slotId:guid}")]
    [ProducesResponseType(typeof(CommonResponse<SlotResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommonResponse<SlotResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSlot(Guid mentorId, Guid slotId, [FromBody] UpdateSlotRequest request)
    {
        var userId = GetUserIdFromClaim();
        if (userId == null)
            return Unauthorized(new CommonResponse<SlotResponseDto> { IsSuccess = false, Message = "Invalid user context" });
        if (userId != mentorId)
            return StatusCode(StatusCodes.Status403Forbidden, new CommonResponse<SlotResponseDto> { IsSuccess = false, Message = "You can only update your own slots" });
        if (request == null)
            return BadRequest(new CommonResponse<SlotResponseDto> { IsSuccess = false, Message = "Request body is required" });
        var result = await _bookingService.UpdateSlotAsync(mentorId, slotId, request);
        if (!result.IsSuccess)
            return result.Message == "Slot not found" ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>Delete a slot. Mentor only. Cannot delete booked slots.</summary>
    [HttpDelete("{slotId:guid}")]
    [ProducesResponseType(typeof(CommonResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteSlot(Guid mentorId, Guid slotId)
    {
        var userId = GetUserIdFromClaim();
        if (userId == null)
            return Unauthorized(new CommonResponse<bool> { IsSuccess = false, Message = "Invalid user context" });
        if (userId != mentorId)
            return StatusCode(StatusCodes.Status403Forbidden, new CommonResponse<bool> { IsSuccess = false, Message = "You can only delete your own slots" });
        var result = await _bookingService.DeleteSlotAsync(mentorId, slotId);
        if (!result.IsSuccess)
            return result.Message == "Slot not found" ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }
}
