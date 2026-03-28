using MeetingService.Application.DTOs;
using MeetingService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Common.Wrappers;
using SharedContracts.Common.Wrappers.Requests;

namespace MeetingService.Api.Controllers;

[ApiController]
[Route("api/meeting")]
[Authorize]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingService _meetingService;

    public MeetingsController(IMeetingService meetingService)
    {
        _meetingService = meetingService;
    }

    /// <summary>Danh sách meeting (phân trang), lọc theo status nếu có (0 Pending, 1 OnGoing, 2 Finished).</summary>
    [HttpGet("meetings")]
    [ProducesResponseType(typeof(CommonResponse<PaginationResponse<MeetingListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMeetings(
        [FromQuery] PaginationRequest? pagination,
        [FromQuery] int? status,
        CancellationToken cancellationToken)
    {
        pagination ??= new PaginationRequest();
        var result = await _meetingService.GetMeetingsPagedAsync(pagination, status, cancellationToken);
        return Ok(result);
    }

    /// <summary>Chi tiết meeting theo Id (kèm danh sách recording).</summary>
    [HttpGet("meetings/{id:guid}")]
    [ProducesResponseType(typeof(CommonResponse<MeetingDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommonResponse<MeetingDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMeetingById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _meetingService.GetMeetingByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Meeting theo BookingId (một booking một meeting).</summary>
    [HttpGet("by-booking/{bookingId:guid}")]
    [ProducesResponseType(typeof(CommonResponse<MeetingDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommonResponse<MeetingDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMeetingByBookingId(Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await _meetingService.GetMeetingByBookingIdAsync(bookingId, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Danh sách recording của một meeting.</summary>
    [HttpGet("meetings/{meetingId:guid}/recordings")]
    [ProducesResponseType(typeof(CommonResponse<List<MeetingRecordingDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommonResponse<List<MeetingRecordingDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRecordingsByMeetingId(Guid meetingId, CancellationToken cancellationToken)
    {
        var result = await _meetingService.GetRecordingsByMeetingIdAsync(meetingId, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    /// <summary>Chi tiết một recording.</summary>
    [HttpGet("recordings/{id:guid}")]
    [ProducesResponseType(typeof(CommonResponse<MeetingRecordingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommonResponse<MeetingRecordingDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRecordingById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _meetingService.GetRecordingByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }
}
