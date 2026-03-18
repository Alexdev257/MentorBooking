using BookingService.Application.Interfaces.Repositories;
using BookingService.Domain.Entities;
using BookingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Infrastructure.Persistence.Repositories;
using Shared.Kernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingService.Infrastructure.Implements.Repositories
{
    public class UnitOfWork : IBookingUnitOfWork
    {
        private readonly BookingApplicationDbContext _context;
        private IDbContextTransaction? _currentTransaction;
        public UnitOfWork(BookingApplicationDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<AvailabilitySlot> AvailabilitySlots => new GenericRepository<AvailabilitySlot>(_context);
        public IGenericRepository<Booking> Bookings => new GenericRepository<Booking>(_context);
        public IGenericRepository<Job> Jobs => new GenericRepository<Job>(_context);

        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                return; // Đã có transaction đang chạy thì không tạo mới
            }

            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                // Luôn SaveChanges trước khi Commit
                await _context.SaveChangesAsync();

                if (_currentTransaction != null)
                {
                    await _currentTransaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw; // Ném lỗi ra để Middleware xử lý
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task RollbackTransactionAsync()
        {
            try
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.RollbackAsync();
                }
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
