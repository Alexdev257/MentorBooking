using AIService.Domain.Entities;
using Google;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence.Interceptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIService.Infrastructure.Persistence
{
    public class AIApplicationDbContext : DbContext
    {
        private readonly AuditableEntityInterceptor? _auditableEntityInterceptor;

        public AIApplicationDbContext()
        {
        }

        /// <summary>
        /// Design-time constructor (e.g. for migrations). Runtime dùng constructor có AuditableEntityInterceptor.
        /// </summary>
        public AIApplicationDbContext(DbContextOptions<AIApplicationDbContext> options) : base(options)
        {
        }

        public AIApplicationDbContext(DbContextOptions<AIApplicationDbContext> options,
            AuditableEntityInterceptor auditableEntityInterceptor) : base(options)
        {
            _auditableEntityInterceptor = auditableEntityInterceptor;
        }

        public virtual DbSet<ActionItem> ActionItems { get; set; }
        public virtual DbSet<MeetingSummary> MeetingSummaries { get; set; }
        public virtual DbSet<AudioTranscript> AudioTranscripts { get; set; }
        public virtual DbSet<AudioTranscriptSegment> AudioTranscriptSegments { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_auditableEntityInterceptor != null)
                optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AIApplicationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
