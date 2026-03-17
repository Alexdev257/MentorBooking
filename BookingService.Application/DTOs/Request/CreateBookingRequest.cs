using System.ComponentModel.DataAnnotations;

namespace BookingService.Application.DTOs.Request;

public class CreateBookingRequest
{
    [Required]
    public Guid MentorId { get; set; }

    [Required]
    public Guid SlotId { get; set; }

    [MaxLength(500)]
    public string? Topic { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public decimal PriceAmount { get; set; } = 0;

    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    public List<string> InvitedEmails { get; set; } = new();
}
