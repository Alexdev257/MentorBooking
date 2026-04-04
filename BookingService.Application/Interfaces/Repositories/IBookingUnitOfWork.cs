using BookingService.Domain.Entities;
using Shared.Kernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingService.Application.Interfaces.Repositories
{
    public interface IBookingUnitOfWork : IUnitOfWork
    {
        IGenericRepository<AvailabilitySlot> AvailabilitySlots { get; }
        IGenericRepository<Booking> Bookings { get; }
        IGenericRepository<BookingParticipant> BookingParticipants { get; }
        IGenericRepository<Job> Jobs { get; }
    }
}
