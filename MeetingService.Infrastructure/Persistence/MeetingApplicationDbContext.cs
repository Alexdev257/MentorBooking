using Google;
using MeetingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence.Interceptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetingService.Infrastructure.Persistence
{
    public class MeetingApplicationDbContext : DbContext
    {
        private readonly AuditableEntityInterceptor _auditableEntityInterceptor;

        public MeetingApplicationDbContext()
        {
        }
        public MeetingApplicationDbContext(DbContextOptions<MeetingApplicationDbContext> options,
            AuditableEntityInterceptor auditableEntityInterceptor) : base(options)
        {
            _auditableEntityInterceptor = auditableEntityInterceptor;
        }

        public virtual DbSet<Meeting> Meetings { get; set; }
        public virtual DbSet<MeetingRecording> MeetingRecordings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MeetingApplicationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
