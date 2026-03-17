using AIService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIService.Infrastructure.Persistence.Configurations;

internal class AudioTranscriptSegmentConfiguration : IEntityTypeConfiguration<AudioTranscriptSegment>
{
    public void Configure(EntityTypeBuilder<AudioTranscriptSegment> builder)
    {
        builder.ToTable("audio_transcript_segments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AudioTranscriptId).IsRequired().HasColumnName("audio_transcript_id");
        builder.Property(x => x.StartSeconds).IsRequired().HasColumnName("start_seconds");
        builder.Property(x => x.EndSeconds).IsRequired().HasColumnName("end_seconds");
        builder.Property(x => x.Text).IsRequired().HasColumnType("text").HasColumnName("text");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.IsDeleted).IsRequired().HasColumnName("is_deleted").HasDefaultValue(false);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
