namespace BookingService.Application.DTOs.Response;

public class BookingResponseDto
{
    public Guid Id { get; set; }
    public Guid MentorId { get; set; }
    public Guid MenteeId { get; set; }
    public Guid? SlotId { get; set; }
    public int Status { get; set; }
    public string? Topic { get; set; }
    public string? Notes { get; set; }
    public decimal PriceAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ScheduleStart { get; set; }
    public DateTime ScheduleEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}
