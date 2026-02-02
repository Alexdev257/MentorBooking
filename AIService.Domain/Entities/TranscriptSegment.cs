using Shared.Kernel.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIService.Domain.Entities
{
    public class TranscriptSegment : AuditableEntity
    {
        public Guid TranscriptId { get; set; }
        public virtual Transcript Transcript { get; set; }

        public string? SpeakerLabel { get; set; }
        public int StartMs { get; set; }
        public int EndMs { get; set; }
        public string Text { get; set; }
        public decimal? Confidence { get; set; }
    }
}
