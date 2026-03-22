namespace AIService.Application.Configuration;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gemini-2.5-flash-lite";

    public int MaxInputCharacters { get; set; } = 500_000;
}
