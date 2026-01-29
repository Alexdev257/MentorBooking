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
    public class UserLocationConfiguration : IEntityTypeConfiguration<UserLocation>
    {
        public void Configure(EntityTypeBuilder<UserLocation> builder)
        {
            builder.ToTable("UserLocation");

            builder.HasKey(ul => ul.Id);
            builder.Property(ul => ul.Id)
                .HasColumnName("id")
                .HasMaxLength(255)
                .HasConversion<string>();

            builder.Property(ul => ul.Latitude).HasColumnType("decimal(18,8)");
            builder.Property(ul => ul.Longitude).HasColumnType("decimal(18,8)");

            builder.HasOne(ul => ul.User)
                   .WithMany(u => u.Locations)
                   .HasForeignKey(ul => ul.UserId)
                   .OnDelete(DeleteBehavior.NoAction);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        }
    }
}
