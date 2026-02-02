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
    public class TranscriptConfiguration : IEntityTypeConfiguration<Transcript>
    {
        public void Configure(EntityTypeBuilder<Transcript> builder)
        {
            throw new NotImplementedException();
        }
    }
}
