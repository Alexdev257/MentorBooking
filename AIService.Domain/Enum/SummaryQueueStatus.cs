namespace AIService.Domain.Enum;

/// <summary>Background summarization job state (Gemini).</summary>
public enum SummaryQueueStatus
{
    None = 0,
    Pending = 1,
    Processing = 2,
    Failed = 3
}
