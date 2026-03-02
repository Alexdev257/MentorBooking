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
    internal class ActionItemConfiguration : IEntityTypeConfiguration<ActionItem>
    {
        public void Configure(EntityTypeBuilder<ActionItem> builder)
        {
            builder.ToTable("ActionItem");

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

            builder.Property(x => x.AssigneeUserId)
                .IsRequired()
                .HasColumnName("assignee_user_id")
                .HasMaxLength(255)
                .HasConversion<string>();

            builder.Property(u => u.Text)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("text");

            builder.Property(u => u.DueAt)
                .HasColumnName("due_at");

            builder.Property(x => x.Status).HasColumnName("status");

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
