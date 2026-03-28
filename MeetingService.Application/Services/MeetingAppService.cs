using MeetingService.Application.DTOs;
using MeetingService.Application.Interfaces.Repositories;
using MeetingService.Application.Interfaces.Services;
using MeetingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Common.Wrappers;
using SharedContracts.Common.Wrappers.Requests;

namespace MeetingService.Application.Services;

public class MeetingAppService : IMeetingService
{
    private readonly IMeetingUnitOfWork _unitOfWork;

    public MeetingAppService(IMeetingUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private static string MapMeetingStatusLabel(int status) => status switch
    {
        0 => "Pending",
        1 => "OnGoing",
        2 => "Finished",
        _ => "Unknown"
    };

    private static MeetingRecordingDto MapRecording(MeetingRecording r) => new()
    {
        Id = r.Id,
        MeetingId = r.MeetingId,
        Status = r.Status,
        StorageUrl = r.StorageUrl,
        ContentType = r.ContentType,
        DurationSeconds = r.DurationSeconds,
        SizeBytes = r.SizeBytes,
        CreatedAt = r.CreatedAt
    };

    private static MeetingDetailDto MapMeetingDetail(Meeting m, bool includeRecordings)
    {
        var dto = new MeetingDetailDto
        {
            Id = m.Id,
            BookingId = m.BookingId,
            Status = m.Status,
            StatusLabel = MapMeetingStatusLabel(m.Status),
            Provider = m.Provider,
            JoinUrl = m.JoinUrl,
            HostUrl = m.HostUrl,
            StartedAt = m.StartedAt,
            EndedAt = m.EndedAt,
            CreatedAt = m.CreatedAt
        };
        if (includeRecordings && m.Recordings != null)
            dto.Recordings = m.Recordings.Where(r => !r.IsDeleted).Select(MapRecording).ToList();
        return dto;
    }

    public async Task<CommonResponse<MeetingDetailDto>> GetMeetingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var meeting = await _unitOfWork.Meetings
            .FindAsync(m => m.Id == id && !m.IsDeleted)
            .Include(m => m.Recordings)
            .FirstOrDefaultAsync(cancellationToken);

        if (meeting == null)
            return new CommonResponse<MeetingDetailDto> { IsSuccess = false, Message = "Không tìm thấy meeting." };

        return new CommonResponse<MeetingDetailDto>
        {
            Data = MapMeetingDetail(meeting, includeRecordings: true)
        };
    }

    public async Task<CommonResponse<MeetingDetailDto>> GetMeetingByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var meeting = await _unitOfWork.Meetings
            .FindAsync(m => m.BookingId == bookingId && !m.IsDeleted)
            .Include(m => m.Recordings)
            .FirstOrDefaultAsync(cancellationToken);

        if (meeting == null)
            return new CommonResponse<MeetingDetailDto> { IsSuccess = false, Message = "Không tìm thấy meeting cho booking này." };

        return new CommonResponse<MeetingDetailDto>
        {
            Data = MapMeetingDetail(meeting, includeRecordings: true)
        };
    }

    public async Task<CommonResponse<PaginationResponse<MeetingListItemDto>>> GetMeetingsPagedAsync(
        PaginationRequest pagination,
        int? status,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Meetings.FindAsync(m => !m.IsDeleted);
        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);

        var meetings = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Include(m => m.Recordings)
            .ToListAsync(cancellationToken);

        var items = meetings.Select(m => new MeetingListItemDto
        {
            Id = m.Id,
            BookingId = m.BookingId,
            Status = m.Status,
            StatusLabel = MapMeetingStatusLabel(m.Status),
            Provider = m.Provider,
            JoinUrl = m.JoinUrl,
            StartedAt = m.StartedAt,
            EndedAt = m.EndedAt,
            CreatedAt = m.CreatedAt,
            RecordingsCount = m.Recordings.Count(r => !r.IsDeleted)
        }).ToList();

        return new CommonResponse<PaginationResponse<MeetingListItemDto>>
        {
            Data = new PaginationResponse<MeetingListItemDto>
            {
                Items = items,
                TotalItems = total,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            }
        };
    }

    public async Task<CommonResponse<List<MeetingRecordingDto>>> GetRecordingsByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var exists = await _unitOfWork.Meetings.AnyAsync(m => m.Id == meetingId && !m.IsDeleted);
        if (!exists)
            return new CommonResponse<List<MeetingRecordingDto>> { IsSuccess = false, Message = "Không tìm thấy meeting." };

        var rows = await _unitOfWork.MeetingRecordings
            .FindAsync(r => r.MeetingId == meetingId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return new CommonResponse<List<MeetingRecordingDto>> { Data = rows.Select(MapRecording).ToList() };
    }

    public async Task<CommonResponse<MeetingRecordingDto>> GetRecordingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rec = await _unitOfWork.MeetingRecordings
            .FindAsync(r => r.Id == id && !r.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (rec == null)
            return new CommonResponse<MeetingRecordingDto> { IsSuccess = false, Message = "Không tìm thấy recording." };

        return new CommonResponse<MeetingRecordingDto> { Data = MapRecording(rec) };
    }
}
