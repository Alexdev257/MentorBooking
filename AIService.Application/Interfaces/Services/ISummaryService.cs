using AIService.Application.DTOs.Summary;

namespace AIService.Application.Interfaces.Services;

public interface ISummaryService
{
    Task<SummaryResponseDto?> SummarizeAsync(string transcriptText, CancellationToken cancellationToken = default);
}
