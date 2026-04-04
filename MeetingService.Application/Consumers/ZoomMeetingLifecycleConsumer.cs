using MassTransit;
using MeetingService.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace MeetingService.Application.Consumers;

public class ZoomMeetingLifecycleConsumer : IConsumer<ZoomMeetingLifecycleEvent>
{
    private readonly IMeetingUnitOfWork _unitOfWork;
    private readonly ILogger<ZoomMeetingLifecycleConsumer> _logger;

    public ZoomMeetingLifecycleConsumer(IMeetingUnitOfWork unitOfWork, ILogger<ZoomMeetingLifecycleConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ZoomMeetingLifecycleEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "ZoomMeetingLifecycle: BookingId={BookingId}, MeetingStatus={Status}",
            message.BookingId,
            message.MeetingStatus);

        var meeting = await _unitOfWork.Meetings
            .FindAsync(m => m.BookingId == message.BookingId)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (meeting == null)
        {
            _logger.LogWarning("No Meeting row for BookingId {BookingId}; skip lifecycle update.", message.BookingId);
            return;
        }

        if (message.MeetingStatus == 1)
        {
            if (meeting.Status == 0)
            {
                meeting.Status = 1;
                _unitOfWork.Meetings.UpdateAsync(meeting);
                await _unitOfWork.SaveChangesAsync(context.CancellationToken);
                _logger.LogInformation("Meeting {MeetingId} set to On-going (1).", meeting.Id);
            }
            return;
        }

        if (message.MeetingStatus == 2)
        {
            meeting.Status = 2;
            _unitOfWork.Meetings.UpdateAsync(meeting);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("Meeting {MeetingId} set to Finished (2).", meeting.Id);
        }
    }
}
