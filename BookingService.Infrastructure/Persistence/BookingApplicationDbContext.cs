using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence.Interceptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingService.Infrastructure.Persistence
{
    public class BookingApplicationDbContext : DbContext
    {
        private readonly AuditableEntityInterceptor _auditableEntityInterceptor;

        //public BookingApplicationDbContext()
        //{
        //}
        public BookingApplicationDbContext(DbContextOptions<BookingApplicationDbContext> options,
            AuditableEntityInterceptor auditableEntityInterceptor) : base(options)
        {
            _auditableEntityInterceptor = auditableEntityInterceptor;
        }

        public virtual DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
        public virtual DbSet<Booking> Bookings { get; set; }
        public virtual DbSet<Job> Jobs { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingApplicationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
