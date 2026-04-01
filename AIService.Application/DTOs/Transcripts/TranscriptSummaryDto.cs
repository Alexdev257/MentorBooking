namespace AIService.Application.DTOs.Transcripts;

public class TranscriptSummaryDto
{
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
    public List<string> Topics { get; set; } = new();

    /// <summary>Raw JSON object string from the model (e.g. overall tone, notes).</summary>
    public string SentimentJson { get; set; } = "{}";

    public string? Model { get; set; }
    public DateTime GeneratedAtUtc { get; set; }

    /// <summary>Structured report JSON: { title, agenda[], decisions[], actionItems[], followUps[], highlights[] }</summary>
    public string? ReportJson { get; set; }

    /// <summary>Mindmap JSON: { centralTopic, branches[{ topic, subtopics[] }] }</summary>
    public string? MindmapJson { get; set; }
}
