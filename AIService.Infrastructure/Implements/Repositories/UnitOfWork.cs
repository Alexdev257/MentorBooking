using AIService.Application.Interfaces.Repositories;
using AIService.Domain.Entities;
using AIService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Infrastructure.Persistence.Repositories;
using Shared.Kernel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIService.Infrastructure.Implements.Repositories
{
    public class UnitOfWork : IAIUnitOfWork
    {
        private readonly AIApplicationDbContext _context;
        private IDbContextTransaction? _currentTransaction;
        public UnitOfWork(AIApplicationDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<ActionItem> ActionItems => new GenericRepository<ActionItem>(_context);
        public IGenericRepository<MeetingSummary> MeetingSummaries => new GenericRepository<MeetingSummary>(_context);
        public IGenericRepository<AudioTranscript> Transcripts => new GenericRepository<AudioTranscript>(_context);
        public IGenericRepository<AudioTranscriptSegment> TranscriptSegments => new GenericRepository<AudioTranscriptSegment>(_context);

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
