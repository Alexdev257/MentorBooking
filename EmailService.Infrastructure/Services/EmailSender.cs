using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace EmailService.Infrastructure.Services;

/// <summary>
/// Gửi email qua Mailjet REST API (HTTPS) — phù hợp với môi trường chặn SMTP (vd. Render free).
/// </summary>
public sealed class EmailSender
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public EmailSender(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Mailjet:ApiKey"] ?? throw new InvalidOperationException("Mailjet:ApiKey is not configured.");
        var secretKey = _configuration["Mailjet:ApiSecret"]
            ?? _configuration["Mailjet:SecretKey"]
            ?? throw new InvalidOperationException("Mailjet:ApiSecret is not configured.");
        var from = _configuration["Mailjet:FromEmail"] ?? throw new InvalidOperationException("Mailjet:FromEmail is not configured.");
        var displayName = _configuration["Mailjet:DisplayName"];

        var sendEndpoint = _configuration["Mailjet:SendEndpoint"] ?? "https://api.mailjet.com/v3.1/send";

        var payload = new MailjetSendRequest
        {
            Messages =
            [
                new MailjetMessage
                {
                    From = new MailjetAddress { Email = from, Name = displayName },
                    To = [new MailjetAddress { Email = to }],
                    Subject = subject,
                    HTMLPart = body,
                },
            ],
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, sendEndpoint);
        var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apiKey}:{secretKey}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        request.Content = JsonContent.Create(payload, options: SerializerOptions);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Mailjet HTTP {(int)response.StatusCode}: {responseText}");

        using var doc = JsonDocument.Parse(responseText);
        var first = doc.RootElement.GetProperty("Messages")[0];
        if (first.TryGetProperty("Errors", out var errs) && errs.GetArrayLength() > 0)
            throw new InvalidOperationException($"Mailjet Errors: {responseText}");

        var status = first.GetProperty("Status").GetString();
        if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Mailjet send not successful (Status={status}): {responseText}");
    }
}

internal sealed class MailjetSendRequest
{
    public List<MailjetMessage> Messages { get; init; } = new();
}

internal sealed class MailjetMessage
{
    public required MailjetAddress From { get; init; }
    public required List<MailjetAddress> To { get; init; }
    public required string Subject { get; init; }
    public required string HTMLPart { get; init; }
}

internal sealed class MailjetAddress
{
    public required string Email { get; init; }
    public string? Name { get; init; }
}
