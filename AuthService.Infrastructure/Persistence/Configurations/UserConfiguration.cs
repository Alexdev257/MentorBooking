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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasMaxLength(255)
                .HasConversion<string>();

            builder.Property(x => x.FullName).HasColumnName("fullname").HasMaxLength(255);
            builder.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
            builder.HasIndex(x => x.Email).IsUnique();
            builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(255);
            builder.Property(x => x.IsVerified).HasColumnName("is_verified").IsRequired();
            builder.Property(x => x.Password).HasColumnName("password").IsRequired().HasMaxLength(255);
            builder.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(255);
            builder.Property(x => x.Gender).HasColumnName("gender").IsRequired();
            builder.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");
            builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(500);
            builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(255);

            builder.Property(x => x.RoleId)
                .HasColumnName("role_id")
                .HasConversion<string>();

            builder.HasOne(x => x.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.OrganizerId)
                .HasColumnName("organizer_id")
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

            builder.Ignore(x => x.DomainEvents);
        }
    }
}
