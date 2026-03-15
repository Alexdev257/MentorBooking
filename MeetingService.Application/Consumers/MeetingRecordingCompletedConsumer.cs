using MassTransit;
using MeetingService.Domain.Entities;
using Shared.Contracts.Events;
using MeetingService.Application.Interfaces.Repositories;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MeetingService.Application.Consumers
{
    public class MeetingRecordingCompletedConsumer : IConsumer<MeetingRecordingCompletedEvent>
    {
        private readonly IMeetingUnitOfWork _unitOfWork;
        private readonly ILogger<MeetingRecordingCompletedConsumer> _logger;

        public MeetingRecordingCompletedConsumer(IMeetingUnitOfWork unitOfWork, ILogger<MeetingRecordingCompletedConsumer> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<MeetingRecordingCompletedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation($"Received MeetingRecordingCompletedEvent for MeetingId: {message.MeetingId}");

            var meeting = await _unitOfWork.Meetings.GetByIdAsync(message.MeetingId);
            if (meeting == null)
            {
                _logger.LogWarning($"Meeting with Id {message.MeetingId} not found.");
                return;
            }

            var recording = new MeetingRecording
            {
                Id = Guid.NewGuid(),
                MeetingId = message.MeetingId,
                StorageUrl = message.StorageUrl,
                Status = 1, // Completed
                ContentType = "video/mp4",
                DurationSeconds = message.DurationSeconds,
                SizeBytes = message.SizeBytes
            };

            await _unitOfWork.MeetingRecordings.AddAsync(recording);
            
            meeting.Status = 2; // Finished
            _unitOfWork.Meetings.UpdateAsync(meeting);

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Created MeetingRecording with Id: {recording.Id}");
        }
    }
}
