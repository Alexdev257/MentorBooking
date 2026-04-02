using AIService.Domain.Entities;
using AIService.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIService.Infrastructure.Persistence.Configurations;

internal class AudioTranscriptConfiguration : IEntityTypeConfiguration<AudioTranscript>
{
    public void Configure(EntityTypeBuilder<AudioTranscript> builder)
    {
        builder.ToTable("audio_transcripts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(500).HasColumnName("title");
        builder.Property(x => x.SourceType).IsRequired().HasColumnName("source_type");
        builder.Property(x => x.OriginalFileName).HasMaxLength(500).HasColumnName("original_file_name");
        builder.Property(x => x.OriginalFilePath).HasMaxLength(2000).HasColumnName("original_file_path");
        builder.Property(x => x.SourceUrl).HasMaxLength(2000).HasColumnName("source_url");
        builder.Property(x => x.ExtractedAudioPath).HasMaxLength(2000).HasColumnName("extracted_audio_path");
        builder.Property(x => x.MimeType).HasMaxLength(100).HasColumnName("mime_type");
        builder.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes");
        builder.Property(x => x.RawText).HasColumnType("text").HasColumnName("raw_text");
        builder.Property(x => x.CleanText).HasColumnType("text").HasColumnName("clean_text");
        builder.Property(x => x.Status).IsRequired().HasColumnName("status");
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000).HasColumnName("error_message");
        builder.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc");
        builder.Property(x => x.SummaryQueueStatus)
            .IsRequired()
            .HasColumnName("summary_queue_status")
            .HasDefaultValue(SummaryQueueStatus.None);
        builder.Property(x => x.SummaryQueueError).HasMaxLength(2000).HasColumnName("summary_queue_error");
        builder.Property(x => x.SummaryRequestedBy).HasColumnName("summary_requested_by");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.IsDeleted).IsRequired().HasColumnName("is_deleted").HasDefaultValue(false);

        builder.HasMany(x => x.Segments)
            .WithOne(x => x.AudioTranscript)
            .HasForeignKey(x => x.AudioTranscriptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
