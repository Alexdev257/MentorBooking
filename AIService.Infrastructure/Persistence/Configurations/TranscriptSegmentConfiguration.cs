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
    public class TranscriptSegmentConfiguration : IEntityTypeConfiguration<TranscriptSegment>
    {
        public void Configure(EntityTypeBuilder<TranscriptSegment> builder)
        {
            builder.ToTable("TranscriptSegment");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasMaxLength(255)
                .HasConversion<string>();

            builder.Property(x => x.TranscriptId)
                .HasColumnName("transcript_id");

            builder.HasOne(t => t.Transcript)
                .WithMany(t => t.Segments)
                .HasForeignKey(t => t.TranscriptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(u => u.SpeakerLabel)
                .HasColumnName("speaker_label");

            builder.Property(u => u.StartMs)
                .HasColumnName("start_ms");

            builder.Property(u => u.EndMs)
                .HasColumnName("=end_ms");

            builder.Property(u => u.Text)
                .HasColumnName("text");

            builder.Property(u => u.Confidence)
                .HasColumnName("confidence");

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
