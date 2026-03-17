namespace AIService.Application.DTOs.Summary;

public class SummaryResponseDto
{
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
    public List<string> Topics { get; set; } = new();
    public string Sentiment { get; set; } = "neutral";
    public string Model { get; set; } = string.Empty;
}
