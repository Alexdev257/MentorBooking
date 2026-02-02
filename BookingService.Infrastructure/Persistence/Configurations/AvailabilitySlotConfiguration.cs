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
    public class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
    {
        public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
        {
            builder.ToTable("availability_slots");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.MentorId)
                .IsRequired()
                .HasColumnName("mentor_id");

            builder.Property(x => x.StartAt)
                .IsRequired()
                .HasColumnName("start_at");

            builder.Property(x => x.EndAt)
                .IsRequired()
                .HasColumnName("end_at");

            builder.Property(x => x.IsBooked)
                .IsRequired()
                .HasDefaultValue(false)
                .HasColumnName("is_booked");

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
        }
    }
}
