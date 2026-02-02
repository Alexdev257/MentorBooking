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
    public class MeetingRecordingConfiguration : IEntityTypeConfiguration<MeetingRecording>
    {
        public void Configure(EntityTypeBuilder<MeetingRecording> builder)
        {
            builder.ToTable("meeting_recordings");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.MeetingId)
                .IsRequired()
                .HasColumnName("meeting_id");

            builder.Property(x => x.Status)
                .IsRequired()
                .HasColumnName("status");

            builder.Property(x => x.StorageUrl)
                .IsRequired()
                .HasColumnName("storage_url");

            builder.Property(x => x.ContentType)
                .HasMaxLength(100)
                .HasColumnName("content_type");

            builder.Property(x => x.DurationSeconds).HasColumnName("duration_seconds");

            builder.Property(x => x.SizeBytes).HasColumnName("size_bytes");

            builder.HasOne(r => r.Meeting)
                .WithMany(m => m.Recordings)
                .HasForeignKey(r => r.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

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
