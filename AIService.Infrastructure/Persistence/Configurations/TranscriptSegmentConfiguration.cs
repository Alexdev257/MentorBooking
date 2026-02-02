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
    public class TranscriptSegmentConfiguration : IEntityTypeConfiguration<TranscriptSegment>
    {
        public void Configure(EntityTypeBuilder<TranscriptSegment> builder)
        {
            throw new NotImplementedException();
        }
    }
}
