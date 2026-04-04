namespace BookingService.Application.Interfaces.Services;

/// <summary>
/// Tải file recording/transcript từ URL cloud Zoom (cần Bearer) và upload thẳng lên Firebase Storage
/// với đường dẫn duy nhất theo booking + meeting.
/// </summary>
public interface IMeetingRecordingCloudMirrorService
{
    /// <param name="zoomMeetingNumericId">Meeting number (như <c>google_event_id</c>).</param>
    /// <param name="storageFileLabel">Ví dụ <c>video</c> hoặc <c>transcript</c>.</param>
    /// <param name="extension">Phải gồm dấu chấm, ví dụ <c>.mp4</c>, <c>.vtt</c>.</param>
    /// <returns>URL công khai Firebase (<c>?alt=media</c>) hoặc null nếu chưa cấu hình / lỗi.</returns>
    Task<string?> TryMirrorToFirebaseAsync(
        Guid bookingId,
        string zoomMeetingNumericId,
        string zoomDownloadUrl,
        string? zoomDownloadToken,
        string storageFileLabel,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default);
}
