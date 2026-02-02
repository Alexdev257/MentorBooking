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
    public class MentorProfileConfiguration : IEntityTypeConfiguration<MentorProfile>
    {
        public void Configure(EntityTypeBuilder<MentorProfile> builder)
        {
            builder.ToTable("mentor_profiles");

            builder.HasKey(m => m.MentorId);

            builder.Property(m => m.MentorId).HasColumnName("mentor_id");


            builder.HasOne(m => m.User)
                .WithOne(u => u.MentorProfile)
                .HasForeignKey<MentorProfile>(m => m.MentorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(m => m.Headline).HasColumnName("headline");
            builder.Property(m => m.Bio).HasColumnName("bio");
            builder.Property(m => m.Languages).HasColumnName("languages");

            builder.Property(m => m.HourlyRate)
                .HasColumnType("numeric(12,2)")
                .HasDefaultValue(0)
                .HasColumnName("hourly_rate");

            builder.Property(m => m.Currency)
                .HasMaxLength(10)
                .HasDefaultValue("VND")
                .HasColumnName("currency");

            builder.Property(m => m.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
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
