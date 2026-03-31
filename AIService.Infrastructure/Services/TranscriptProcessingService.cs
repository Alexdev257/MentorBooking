using AIService.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIService.Infrastructure.Services;

public class TranscriptProcessingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TranscriptProcessingService> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    public TranscriptProcessingService(
        IServiceScopeFactory scopeFactory,
        ILogger<TranscriptProcessingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the application to fully start before polling
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        _logger.LogInformation("TranscriptProcessingService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedTranscriptsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in transcript processing loop");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("TranscriptProcessingService stopped");
    }

    private async Task ProcessQueuedTranscriptsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var transcriptService = scope.ServiceProvider.GetRequiredService<ITranscriptService>();

        var queuedIds = await transcriptService.GetQueuedTranscriptIdsAsync(stoppingToken);

        if (queuedIds.Count == 0)
            return;

        _logger.LogInformation("Found {Count} queued transcript(s) to process", queuedIds.Count);

        foreach (var id in queuedIds)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            _logger.LogInformation("Processing transcript {TranscriptId}", id);

            try
            {
                await transcriptService.ProcessTranscriptAsync(id, stoppingToken);
                _logger.LogInformation("Transcript {TranscriptId} processed successfully", id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to process transcript {TranscriptId}", id);
            }
        }
    }
}
