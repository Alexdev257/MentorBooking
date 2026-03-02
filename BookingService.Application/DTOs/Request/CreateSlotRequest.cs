using System.ComponentModel.DataAnnotations;

namespace BookingService.Application.DTOs.Request;

public class CreateSlotRequest
{
    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }
}
