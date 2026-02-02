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
        private readonly AuditableEntityInterceptor _auditableEntityInterceptor;

        public AIApplicationDbContext()
        {
        }
        public AIApplicationDbContext(DbContextOptions<AIApplicationDbContext> options,
            AuditableEntityInterceptor auditableEntityInterceptor) : base(options)
        {
            _auditableEntityInterceptor = auditableEntityInterceptor;
        }

        public virtual DbSet<ActionItem> ActionItems { get; set; }
        public virtual DbSet<MeetingSummary> MeetingSummaries { get; set; }
        public virtual DbSet<Transcript> Transcripts { get; set; }
        public virtual DbSet<TranscriptSegment> TranscriptSegments { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AIApplicationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
