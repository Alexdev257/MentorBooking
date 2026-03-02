using BookingService.Application.DTOs.Request;
using BookingService.Application.DTOs.Response;
using Shared.Contracts.Common.Wrappers;
using SharedContracts.Common.Wrappers.Requests;

namespace BookingService.Application.Interfaces.Services;

public interface IBookingService
{
    Task<CommonResponse<List<SlotResponseDto>>> GetAvailableSlotsAsync(Guid mentorId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    Task<CommonResponse<SlotResponseDto?>> GetSlotByIdAsync(Guid mentorId, Guid slotId, CancellationToken cancellationToken = default);
    Task<CommonResponse<SlotResponseDto>> CreateSlotAsync(Guid mentorId, CreateSlotRequest request, CancellationToken cancellationToken = default);
    Task<CommonResponse<SlotResponseDto>> UpdateSlotAsync(Guid mentorId, Guid slotId, UpdateSlotRequest request, CancellationToken cancellationToken = default);
    Task<CommonResponse<bool>> DeleteSlotAsync(Guid mentorId, Guid slotId, CancellationToken cancellationToken = default);

    Task<CommonResponse<BookingResponseDto>> CreateBookingAsync(Guid menteeId, CreateBookingRequest request, CancellationToken cancellationToken = default);
    Task<CommonResponse<BookingResponseDto?>> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<CommonResponse<PaginationResponse<BookingResponseDto>>> GetMenteeBookingsAsync(Guid menteeId, PaginationRequest request, CancellationToken cancellationToken = default);
    Task<CommonResponse<PaginationResponse<BookingResponseDto>>> GetMentorBookingsAsync(Guid mentorId, PaginationRequest request, int? status, CancellationToken cancellationToken = default);
    Task<CommonResponse<BookingResponseDto>> AcceptBookingAsync(Guid bookingId, Guid mentorId, CancellationToken cancellationToken = default);
    Task<CommonResponse<BookingResponseDto>> RejectBookingAsync(Guid bookingId, Guid mentorId, CancellationToken cancellationToken = default);
    Task<CommonResponse<BookingResponseDto>> CancelBookingAsync(Guid bookingId, Guid userId, bool isMentor, CancellationToken cancellationToken = default);
}
