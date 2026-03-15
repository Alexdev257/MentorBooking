using MediatR;
using Shared.Contracts.Common.Wrappers;
using System;

namespace MeetingService.Application.CQRS.Command
{
    public class JoinMeetingCommand : IRequest<CommonResponse<string>>
    {
        public Guid MeetingId { get; set; }
        public Guid UserId { get; set; } // The ID of the user joining (Mentor/Mentee)
    }
}
