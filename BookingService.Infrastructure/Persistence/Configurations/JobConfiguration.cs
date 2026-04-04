using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingService.Infrastructure.Persistence.Configurations
{
    public class JobConfiguration : IEntityTypeConfiguration<Job>
    {
        public void Configure(EntityTypeBuilder<Job> builder)
        {
            builder.ToTable("jobs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("type");

            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("QUEUED")
                .HasColumnName("status");

            builder.Property(x => x.Payload)
                .IsRequired()
                .HasColumnType("jsonb") 
                .HasDefaultValueSql("'{}'")
                .HasColumnName("payload");

            builder.Property(x => x.Result)
                .HasColumnType("jsonb")
                .HasColumnName("result");

            builder.Property(x => x.ErrorMessage).HasColumnName("error_message");

            builder.Property(x => x.Attempts)
                .IsRequired()
                .HasDefaultValue(0)
                .HasColumnName("attempts");

            builder.Property(x => x.MaxAttempts)
                .IsRequired()
                .HasDefaultValue(5)
                .HasColumnName("max_attempts");

            builder.Property(x => x.RunAfter)
                .IsRequired()
                .HasDefaultValueSql("now()")
                .HasColumnName("run_after");

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
