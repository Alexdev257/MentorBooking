using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BookingService.Application.DTOs.Request;

/// <summary>
/// Zoom đôi khi gửi <c>id</c>/<c>meeting_id</c> là JSON number, đôi khi là string.
/// Nếu ép kiểu thất bại, ASP.NET sẽ trả 400 trước khi vào controller — webhook <c>recording.completed</c> bị Zoom retry / bỏ.
/// </summary>
public sealed class ZoomJsonStringOrNumberConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Null => null,
            JsonTokenType.Number => reader.TryGetInt64(out var n)
                ? n.ToString(CultureInfo.InvariantCulture)
                : reader.GetDecimal().ToString(CultureInfo.InvariantCulture),
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to string for Zoom id field."),
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}
