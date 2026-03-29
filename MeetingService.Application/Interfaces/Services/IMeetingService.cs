using MeetingService.Application.DTOs;
using Shared.Contracts.Common.Wrappers;
using SharedContracts.Common.Wrappers.Requests;

namespace MeetingService.Application.Interfaces.Services;

public interface IMeetingService
{
    Task<CommonResponse<MeetingDetailDto>> GetMeetingByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CommonResponse<MeetingDetailDto>> GetMeetingByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<CommonResponse<PaginationResponse<MeetingListItemDto>>> GetMeetingsPagedAsync(
        PaginationRequest pagination,
        int? status,
        CancellationToken cancellationToken = default);

    Task<CommonResponse<List<MeetingRecordingDto>>> GetRecordingsByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken = default);

    Task<CommonResponse<MeetingRecordingDto>> GetRecordingByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CommonResponse<List<MeetingRecordingDto>>> GetRecordingsByBookingIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<CommonResponse<MeetingJoinLinksDto>> GetMeetingJoinLinksAsync(Guid meetingId, CancellationToken cancellationToken = default);
}
