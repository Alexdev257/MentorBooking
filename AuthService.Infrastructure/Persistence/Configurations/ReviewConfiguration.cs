using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.BookingId).HasColumnName("booking_id").IsRequired();
        builder.HasIndex(e => e.BookingId).IsUnique();

        builder.Property(e => e.MenteeId).HasColumnName("mentee_id").IsRequired();
        builder.Property(e => e.MentorId).HasColumnName("mentor_id").IsRequired();
        builder.Property(e => e.Rating).HasColumnName("rating").IsRequired();
        builder.Property(e => e.Comment).HasColumnName("comment");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired()
            .HasDefaultValueSql("false");

        builder.HasOne(e => e.Mentor)
            .WithMany()
            .HasForeignKey(e => e.MentorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(e => e.Mentee)
            .WithMany()
            .HasForeignKey(e => e.MenteeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(e => e.MentorId);
        builder.HasIndex(e => e.MenteeId);
    }
}
