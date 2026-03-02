namespace BookingService.Application.DTOs.Response;

public class SlotResponseDto
{
    public Guid Id { get; set; }
    public Guid MentorId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsBooked { get; set; }
    public DateTime CreatedAt { get; set; }
}
