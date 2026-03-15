using MassTransit;
using MeetingService.Domain.Entities;
using Shared.Contracts.Events;
using Shared.Contracts.Interfaces;
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
        private readonly IMessageProducer _messageProducer;

        public BookingAcceptedConsumer(IMeetingUnitOfWork unitOfWork, ILogger<BookingAcceptedConsumer> logger, IMessageProducer messageProducer)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageProducer = messageProducer;
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
                StartedAt = message.StartedAt,
                EndedAt = message.EndedAt
            };

            await _unitOfWork.Meetings.AddAsync(meeting);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Created Meeting with Id: {meeting.Id}");
        }
    }
}
