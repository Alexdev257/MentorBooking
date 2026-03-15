using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public record MeetingRecordingCompletedEvent(
        Guid MeetingId,
        string StorageUrl,
        int DurationSeconds,
        long SizeBytes
    ) : IntegrationEvent;
}
