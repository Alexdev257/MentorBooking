using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Persistence.Configurations
{
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.ToTable("reviews");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.BookingId)
                .IsRequired()
                .HasColumnName("booking_id");
            builder.HasIndex(r => r.BookingId).IsUnique();

            builder.Property(r => r.MentorId).HasColumnName("mentor_id");
            builder.Property(r => r.MenteeId).HasColumnName("mentee_id");

            builder.Property(r => r.Rating)
                .IsRequired()
                .HasColumnName("rating");

            builder.Property(r => r.Comment).HasColumnName("comment");

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

            builder.HasOne(r => r.Mentor)
                .WithMany(u => u.ReviewsReceived)
                .HasForeignKey(r => r.MentorId)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.HasOne(r => r.Mentee)
                .WithMany(u => u.ReviewsGiven)
                .HasForeignKey(r => r.MenteeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
