using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingService.Infrastructure.Persistence.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("bookings");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.MentorId).IsRequired().HasColumnName("mentor_id");
            builder.Property(x => x.MenteeId).IsRequired().HasColumnName("mentee_id");
            builder.Property(x => x.SlotId).HasColumnName("slot_id");

            builder.Property(x => x.Status)
                .IsRequired()
                .HasDefaultValue((int)BookingService.Domain.Enum.BookingStatusEnum.Pending)
                .HasColumnName("status");

            builder.Property(x => x.Topic).HasColumnName("topic");
            builder.Property(x => x.Notes).HasColumnName("notes");

            builder.Property(x => x.PriceAmount)
                .IsRequired()
                .HasColumnType("numeric(12,2)")
                .HasDefaultValue(0)
                .HasColumnName("price_amount");

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("VND")
                .HasColumnName("currency");

            builder.Property(x => x.ScheduleStart).IsRequired().HasColumnName("scheduled_start");
            builder.Property(x => x.ScheduleEnd).IsRequired().HasColumnName("scheduled_end");
            builder.Property(x => x.MeetingLink).HasColumnName("meeting_link");
            builder.Property(x => x.GoogleEventId).HasColumnName("google_event_id");

            builder.Property(u => u.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("now()");

            builder.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired(false);
            builder.Property(u => u.DeletedAt)
                .HasColumnName("deleted_at")
                .IsRequired(false);
            builder.Property(u => u.IsDeleted)
                .IsRequired()
                .HasDefaultValueSql("false");

            builder.HasOne(b => b.Slot)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.SlotId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
