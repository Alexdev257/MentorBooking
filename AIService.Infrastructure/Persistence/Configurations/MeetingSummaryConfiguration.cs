using AIService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIService.Infrastructure.Persistence.Configurations
{
    public class MeetingSummaryConfiguration : IEntityTypeConfiguration<MeetingSummary>
    {
        public void Configure(EntityTypeBuilder<MeetingSummary> builder)
        {
            builder.ToTable("MeetingSummary");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasMaxLength(255)
                .HasConversion<string>();

            builder.Property(u => u.MeetingId)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("meeting_id")
                .HasConversion<string>();

            builder.Property(u => u.Summary)
                .HasColumnName("summary");

            builder.Property(u => u.KeyPoints)
                .HasColumnName("key_points");

            builder.Property(u => u.Topics)
                .HasColumnName("=topics");

            builder.Property(u => u.Sentiment)
                .HasColumnName("sentiment");

            builder.Property(u => u.Model)
                .HasColumnName("model");

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
