using MeetingService.Domain.Entities;
using Shared.Kernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetingService.Application.Interfaces.Repositories
{
    public interface IMeetingUnitOfWork : IUnitOfWork
    {
        IGenericRepository<Meeting> Meetings { get; }
        IGenericRepository<MeetingRecording> MeetingRecordings { get; }
    }
}
