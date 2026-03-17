using AIService.Application.Interfaces;
using System.Diagnostics;

namespace AIService.Infrastructure.Services;

public class MediaProcessingService : IMediaProcessingService
{
    public async Task<string> ExtractAudioToWavAsync(string inputFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException("Input file not found.", inputFilePath);

        var dir = Path.GetDirectoryName(inputFilePath) ?? Path.GetTempPath();
        var outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_{Guid.NewGuid():N}.wav";
        var outputPath = Path.Combine(dir, outputFileName);

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{inputFilePath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{outputPath}\" -y",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start ffmpeg.");

        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            var err = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"FFmpeg failed (exit {process.ExitCode}): {err}");
        }

        if (!File.Exists(outputPath))
            throw new InvalidOperationException("FFmpeg did not produce output file.");

        return outputPath;
    }
}
