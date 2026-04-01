using AIService.Application.Interfaces;
using FFMpegCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;

namespace AIService.Infrastructure.Services;

public class FfmpegMediaProcessingService : IMediaProcessingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FfmpegMediaProcessingService> _logger;
    private static bool _ffOptionsConfigured;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    private static readonly string _defaultBinaryFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "MentorBooking", "ffmpeg");

    public FfmpegMediaProcessingService(IConfiguration configuration, ILogger<FfmpegMediaProcessingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private async Task EnsureFFmpegAsync(CancellationToken cancellationToken)
    {
        if (_ffOptionsConfigured) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_ffOptionsConfigured) return;

            var configFolder = _configuration["FFmpeg:BinaryFolder"];

            if (!string.IsNullOrWhiteSpace(configFolder))
            {
                GlobalFFOptions.Configure(opts => opts.BinaryFolder = configFolder);
                _logger.LogInformation("FFmpeg BinaryFolder (from config) = '{Folder}'.", configFolder);
                _ffOptionsConfigured = true;
                return;
            }

            if (IsFFmpegOnPath())
            {
                _logger.LogInformation("FFmpeg found on system PATH.");
                _ffOptionsConfigured = true;
                return;
            }

            var binaryFolder = _defaultBinaryFolder;
            var ffmpegExe = Path.Combine(binaryFolder,
                OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");

            if (!File.Exists(ffmpegExe))
            {
                if (OperatingSystem.IsWindows())
                {
                    _logger.LogInformation(
                        "FFmpeg not found. Auto-downloading (~30s)...");
                    if (!Directory.Exists(binaryFolder))
                        Directory.CreateDirectory(binaryFolder);
                    await DownloadFFmpegWindowsAsync(binaryFolder, _logger, cancellationToken);
                    _logger.LogInformation("FFmpeg downloaded to '{Folder}'.", binaryFolder);
                }
                else
                {
                    throw new InvalidOperationException(
                        "FFmpeg không tìm thấy trên hệ thống. " +
                        "Hãy cài đặt FFmpeg (apt-get install -y ffmpeg) hoặc set env var FFmpeg__BinaryFolder.");
                }
            }

            GlobalFFOptions.Configure(opts => opts.BinaryFolder = binaryFolder);
            _logger.LogInformation("FFmpeg BinaryFolder = '{Folder}'.", binaryFolder);
            _ffOptionsConfigured = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static bool IsFFmpegOnPath()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (process == null) return false;
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task DownloadFFmpegWindowsAsync(string folder, ILogger logger, CancellationToken ct)
    {
        // FFmpeg Windows build nhẹ (lgpl) từ BtbN/FFmpeg-Builds trên GitHub
        const string downloadUrl =
            "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-lgpl.zip";

        var zipPath = Path.Combine(Path.GetTempPath(), $"ffmpeg-dl-{Guid.NewGuid():N}.zip");
        try
        {
            logger.LogInformation("Đang tải FFmpeg từ {Url} ...", downloadUrl);

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            var bytes = await client.GetByteArrayAsync(downloadUrl, ct);
            await File.WriteAllBytesAsync(zipPath, bytes, ct);

            logger.LogInformation("Đang giải nén ffmpeg.exe và ffprobe.exe ...");

            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                var name = Path.GetFileName(entry.FullName);
                if (name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("ffprobe.exe", StringComparison.OrdinalIgnoreCase))
                {
                    var dest = Path.Combine(folder, name);
                    entry.ExtractToFile(dest, overwrite: true);
                    logger.LogInformation("Extracted {Name} -> {Dest}", name, dest);
                }
            }
        }
        finally
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);
        }

        var ffmpegExe = Path.Combine(folder, "ffmpeg.exe");
        if (!File.Exists(ffmpegExe))
            throw new InvalidOperationException(
                $"Tải FFmpeg thất bại: không tìm thấy ffmpeg.exe trong '{folder}' sau khi giải nén. " +
                "Hãy tải thủ công từ https://ffmpeg.org/download.html và đặt vào thư mục đó, sau đó set 'FFmpeg:BinaryFolder' trong appsettings.Development.json.");
    }

    public async Task<string> ExtractAudioToWavAsync(string inputFilePath, CancellationToken cancellationToken = default)
    {
        await EnsureFFmpegAsync(cancellationToken);

        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException("Input file not found.", inputFilePath);

        var dir = Path.GetDirectoryName(inputFilePath) ?? Path.GetTempPath();
        var outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_{Guid.NewGuid():N}.wav";
        var outputPath = Path.Combine(dir, outputFileName);

        await FFMpegArguments
            .FromFileInput(inputFilePath)
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithCustomArgument("-vn -acodec pcm_s16le -ar 16000 -ac 1"))
            .ProcessAsynchronously(true);

        if (!File.Exists(outputPath))
            throw new InvalidOperationException("FFmpeg did not produce output file.");

        return outputPath;
    }

    public async Task<string> ExtractAudioForTranscriptionAsync(string inputFilePath, CancellationToken cancellationToken = default)
    {
        await EnsureFFmpegAsync(cancellationToken);

        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException("Input file not found.", inputFilePath);

        var dir = Path.GetDirectoryName(inputFilePath) ?? Path.GetTempPath();
        var outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_{Guid.NewGuid():N}.mp3";
        var outputPath = Path.Combine(dir, outputFileName);

        try
        {
            await FFMpegArguments
                .FromFileInput(inputFilePath)
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithCustomArgument("-vn -acodec libmp3lame -ar 16000 -ac 1 -b:a 32k"))
                .ProcessAsynchronously(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FFmpeg MP3 encode failed; retrying with AAC.");
            outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_{Guid.NewGuid():N}.m4a";
            outputPath = Path.Combine(dir, outputFileName);
            await FFMpegArguments
                .FromFileInput(inputFilePath)
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithCustomArgument("-vn -acodec aac -ar 16000 -ac 1 -b:a 32k"))
                .ProcessAsynchronously(true);
        }

        if (!File.Exists(outputPath))
            throw new InvalidOperationException("FFmpeg did not produce compressed audio file.");

        return outputPath;
    }
}
