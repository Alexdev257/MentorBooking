using MeetingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetingService.Infrastructure.Persistence.Configurations
{
    public class MeetingConfigurations : IEntityTypeConfiguration<Meeting>
    {
        public void Configure(EntityTypeBuilder<Meeting> builder)
        {
            builder.ToTable("meetings");

            builder.HasKey(x => x.Id);

            // BookingId là Unique (1 Booking chỉ có 1 Meeting room)
            builder.Property(x => x.BookingId)
                .IsRequired()
                .HasColumnName("booking_id");

            builder.HasIndex(x => x.BookingId).IsUnique();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasColumnName("status");

            builder.Property(x => x.Provider)
                .IsRequired()
                .HasMaxLength(30)
                .HasDefaultValue("IN_APP")
                .HasColumnName("provider");

            builder.Property(x => x.JoinUrl).HasColumnName("join_url");
            builder.Property(x => x.HostUrl).HasColumnName("host_url");

            builder.Property(x => x.StartedAt).HasColumnName("started_at");
            builder.Property(x => x.EndedAt).HasColumnName("ended_at");

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
