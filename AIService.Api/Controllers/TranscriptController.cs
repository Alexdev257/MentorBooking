using AIService.Application.DTOs.Transcripts;
using AIService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shared.Contracts.Common.Wrappers;
using System.Security.Claims;

namespace AIService.Api.Controllers;

[ApiController]
[Route("api/transcripts")]
public class TranscriptController : ControllerBase
{
    private readonly ITranscriptService _transcriptService;

    public TranscriptController(ITranscriptService transcriptService)
    {
        _transcriptService = transcriptService;
    }

    private static void FillValidationErrors<T>(CommonResponse<T> response, ModelStateDictionary modelState)
    {
        response.IsSuccess = false;
        response.Message = "Validation failed";
        foreach (var key in modelState.Keys)
            foreach (var err in modelState[key]!.Errors)
                response.ListErrors.Add(new Errors { Field = key ?? "", Detail = err.ErrorMessage });
    }

    private Guid? GetUserIdFromClaim()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("UserId")?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userId, out var id) ? id : null;
    }

    [HttpPost("upload")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CommonResponse<TranscriptUploadResponseDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(CommonResponse<TranscriptUploadResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequestSizeLimit(524_288_000)]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        [FromForm] string? title,
        [FromForm] int sourceType = 1,
        CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<TranscriptUploadResponseDto>();
        if (file == null || file.Length == 0)
        {
            response.IsSuccess = false;
            response.Message = "File is required.";
            response.ListErrors.Add(new Errors { Field = "file", Detail = "Please upload a video or audio file." });
            return BadRequest(response);
        }

        var userId = GetUserIdFromClaim();
        var name = title ?? file.FileName ?? "upload";
        await using var stream = file.OpenReadStream();
        var result = await _transcriptService.UploadAsync(
            name,
            sourceType,
            stream,
            file.FileName ?? "audio",
            file.ContentType ?? "application/octet-stream",
            file.Length,
            userId,
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);
        return StatusCode(StatusCodes.Status202Accepted, result);
    }

    [HttpPost("{id:guid}/summarize")]
    [AllowAnonymous] // TODO: Remove this later
    [ProducesResponseType(typeof(CommonResponse<TranscriptSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommonResponse<TranscriptSummaryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CommonResponse<TranscriptSummaryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Summarize(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromClaim();
        var result = await _transcriptService.SummarizeAsync(id, userId, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Message == "Không tìm thấy transcript.")
                return NotFound(result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CommonResponse<TranscriptDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _transcriptService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(CommonResponse<List<TranscriptListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (pageSize > 100) pageSize = 100;
        var result = await _transcriptService.GetListAsync(pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("upload-from-url")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CommonResponse<TranscriptUploadResponseDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(CommonResponse<TranscriptUploadResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFromUrl(
        [FromBody] UploadFromUrlRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            var bad = new CommonResponse<TranscriptUploadResponseDto> { IsSuccess = false, Message = "url is required." };
            return BadRequest(bad);
        }

        var userId = GetUserIdFromClaim();
        var result = await _transcriptService.UploadFromUrlAsync(
            request.Url, request.Title, request.SourceType, request.ContentType, userId, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);
        return StatusCode(StatusCodes.Status202Accepted, result);
    }

    [HttpPost("ingest/zoom-audio")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CommonResponse<ZoomAudioTranscriptIngestResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CommonResponse<ZoomAudioTranscriptIngestResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestZoomAudioTranscript(
        [FromBody] ZoomAudioTranscriptIngestRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var invalid = new CommonResponse<ZoomAudioTranscriptIngestResponseDto>();
            FillValidationErrors(invalid, ModelState);
            return BadRequest(invalid);
        }

        var userId = GetUserIdFromClaim();
        var result = await _transcriptService.IngestZoomAudioTranscriptAsync(request, userId, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
