using AIService.Application.DTOs.Transcripts;
using AIService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Whisper.net;
using Whisper.net.Ggml;

namespace AIService.Infrastructure.Services;

public class WhisperTranscriptionService : ITranscriptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhisperTranscriptionService> _logger;

    public WhisperTranscriptionService(IConfiguration configuration, ILogger<WhisperTranscriptionService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Nếu file model chưa tồn tại và có bật auto-download, tải model qua WhisperGgmlDownloader (từ Hugging Face).
    /// </summary>
    private async Task<string> EnsureModelExistsAsync(string modelPath, CancellationToken cancellationToken)
    {
        if (File.Exists(modelPath))
            return modelPath;

        var autoDownload = _configuration.GetValue<bool>("Whisper:AutoDownloadIfMissing");
        if (!autoDownload)
            throw new InvalidOperationException(
                "Whisper:ModelPath is not set or file not found. Set Whisper:ModelPath to the path of ggml-base.bin, or set Whisper:AutoDownloadIfMissing to true to download automatically.");

        var modelTypeStr = _configuration["Whisper:ModelType"] ?? "Base";
        var ggmlType = ParseGgmlType(modelTypeStr);

        var dir = Path.GetDirectoryName(modelPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _logger.LogInformation("Downloading Whisper model {ModelType} to {Path}...", modelTypeStr, modelPath);
        await using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType, cancellationToken: cancellationToken);
        await using var fileStream = File.Create(modelPath);
        await modelStream.CopyToAsync(fileStream, cancellationToken);
        _logger.LogInformation("Whisper model downloaded to {Path}.", modelPath);
        return modelPath;
    }

    private static GgmlType ParseGgmlType(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "tiny" => GgmlType.Tiny,
            "tinyen" or "tiny.en" => GgmlType.TinyEn,
            "base" => GgmlType.Base,
            "baseen" or "base.en" => GgmlType.BaseEn,
            "small" => GgmlType.Small,
            "smallen" or "small.en" => GgmlType.SmallEn,
            "medium" => GgmlType.Medium,
            "mediumen" or "medium.en" => GgmlType.MediumEn,
            "large" or "largev1" => GgmlType.LargeV1,
            "largev2" or "large-v2" => GgmlType.LargeV2,
            "largev3" or "large-v3" => GgmlType.LargeV3,
            _ => GgmlType.Base
        };
    }

    public async Task<TranscriptionResult> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(audioFilePath))
            throw new FileNotFoundException("Audio file not found.", audioFilePath);

        var modelPath = _configuration["Whisper:ModelPath"];
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new InvalidOperationException("Whisper:ModelPath is not set in appsettings. Set it to the path where the model file should be (e.g. D:\\Models\\ggml-base.bin).");

        modelPath = await EnsureModelExistsAsync(modelPath, cancellationToken);

        var result = new TranscriptionResult();
        using var whisperFactory = WhisperFactory.FromPath(modelPath);
        using var processor = whisperFactory.CreateBuilder()
            .WithLanguage("auto")
            .Build();

        await using var fileStream = File.OpenRead(audioFilePath);
        await foreach (var segment in processor.ProcessAsync(fileStream, cancellationToken))
        {
            result.Segments.Add(new TranscriptSegmentDto
            {
                StartSeconds = segment.Start.TotalSeconds,
                EndSeconds = segment.End.TotalSeconds,
                Text = segment.Text?.Trim() ?? ""
            });
        }

        result.FullText = string.Join(" ", result.Segments.Select(s => s.Text)).Trim();
        return result;
    }
}
