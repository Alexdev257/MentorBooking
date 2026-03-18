using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Persistence
{
    public class BookingApplicationDbContext : DbContext
    {
        public BookingApplicationDbContext(DbContextOptions<BookingApplicationDbContext> options)
            : base(options) { }

        public virtual DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
        public virtual DbSet<Booking> Bookings { get; set; }
        public virtual DbSet<Job> Jobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingApplicationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
