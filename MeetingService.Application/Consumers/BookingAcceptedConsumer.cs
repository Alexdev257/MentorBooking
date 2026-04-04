using MassTransit;
using MeetingService.Domain.Entities;
using Shared.Contracts.Events;
using MeetingService.Application.Interfaces.Repositories;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MeetingService.Application.Consumers
{
    public class BookingAcceptedConsumer : IConsumer<BookingAcceptedEvent>
    {
        private readonly IMeetingUnitOfWork _unitOfWork;
        private readonly ILogger<BookingAcceptedConsumer> _logger;

        public BookingAcceptedConsumer(IMeetingUnitOfWork unitOfWork, ILogger<BookingAcceptedConsumer> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<BookingAcceptedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation($"Received BookingAcceptedEvent for BookingId: {message.BookingId}");

            var meeting = new Meeting
            {
                Id = Guid.NewGuid(),
                BookingId = message.BookingId,
                Status = 0, // Pending
                Provider = "Zoom",
                JoinUrl = message.JoinUrl,
                HostUrl = message.JoinUrl, // Mentor uses the same link for now as requested
                StartedAt = message.StartedAt.ToUniversalTime(),
                EndedAt = message.EndedAt.ToUniversalTime()
            };

            await _unitOfWork.Meetings.AddAsync(meeting);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Created Meeting with Id: {meeting.Id}");
        }
    }
}
