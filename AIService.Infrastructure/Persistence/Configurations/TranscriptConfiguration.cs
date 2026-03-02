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
    public class TranscriptConfiguration : IEntityTypeConfiguration<Transcript>
    {
        public void Configure(EntityTypeBuilder<Transcript> builder)
        {
            builder.ToTable("Transcript");

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

            builder.Property(u => u.Status)
                .IsRequired()
                .HasColumnName("status");

            builder.Property(u => u.Language)
                .HasMaxLength(255)
                .HasColumnName("language");

            builder.Property(u => u.Model)
                .HasMaxLength(255)
                .HasColumnName("model");

            builder.HasMany(t => t.Segments)
                .WithOne() 
                .HasForeignKey("TranscriptId") 
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
