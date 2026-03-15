using MediatR;
using MeetingService.Application.CQRS.Command;
using MeetingService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Common.Wrappers;
using Shared.Contracts.Events;
using Shared.Contracts.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MeetingService.Application.CQRS.Handler
{
    public class JoinMeetingCommandHandler : IRequestHandler<JoinMeetingCommand, CommonResponse<string>>
    {
        private readonly IMeetingUnitOfWork _unitOfWork;
        private readonly IMessageProducer _messageProducer;
        private readonly ILogger<JoinMeetingCommandHandler> _logger;

        public JoinMeetingCommandHandler(
            IMeetingUnitOfWork unitOfWork, 
            IMessageProducer messageProducer,
            ILogger<JoinMeetingCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageProducer = messageProducer;
            _logger = logger;
        }

        public async Task<CommonResponse<string>> Handle(JoinMeetingCommand request, CancellationToken cancellationToken)
        {
            var meeting = await _unitOfWork.Meetings.GetByIdAsync(request.MeetingId);
            
            if (meeting == null)
            {
                return new CommonResponse<string>
                {
                    IsSuccess = false,
                    Message = "Meeting not found.",
                    Data = null
                };
            }

            // Optional: verify if request.UserId is authorized to join.

            if (meeting.Status == 2) // Finished
            {
                return new CommonResponse<string>
                {
                    IsSuccess = false,
                    Message = "Meeting has already finished.",
                    Data = null
                };
            }

            // Only trigger the bot if this is the FIRST time someone joins (Status == 0)
            if (meeting.Status == 0) // Pending
            {
                meeting.Status = 1; // On-going
                _unitOfWork.Meetings.UpdateAsync(meeting);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if(meeting.StartedAt.HasValue && meeting.StartedAt.Value <= DateTime.UtcNow)
                {
                    _logger.LogInformation($"Meeting {meeting.Id} started. Triggering 10-minute bot delay.");

                    // Delay bot connection by 10 minutes to wait for both parties to join and start speaking.
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromMinutes(10));
                        try
                        {
                            // Double check if meeting wasn't canceled or finished early
                            var currentMeeting = await _unitOfWork.Meetings.GetByIdAsync(meeting.Id);
                            if (currentMeeting != null && currentMeeting.Status == 1)
                            {
                                _logger.LogInformation($"Delay finished. Dispatching RecordMeetingCommand to Bot for MeetingId: {meeting.Id}");
                                // Handle Nullable DateTime values carefully
                                int durationMins = 60; // default to 60mins
                                if (meeting.EndedAt.HasValue && meeting.StartedAt.HasValue)
                                {
                                    durationMins = (int)(meeting.EndedAt.Value - meeting.StartedAt.Value).TotalMinutes;
                                }
                                if (durationMins <= 0) durationMins = 60;

                                var cmd = new RecordMeetingCommand(meeting.Id, meeting.JoinUrl, durationMins);
                                await _messageProducer.PublishAsync(cmd);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error firing delayed RecordMeetingCommand.");
                        }
                    });
                }
            }

            return new CommonResponse<string>
            {
                IsSuccess = true,
                Message = "Join successful.",
                Data = meeting.JoinUrl // Return the actual Google Meet URL for the frontend to redirect
            };
        }
    }
}
