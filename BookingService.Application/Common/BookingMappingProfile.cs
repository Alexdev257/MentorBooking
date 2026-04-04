using AutoMapper;
using BookingService.Application.DTOs.Response;
using BookingService.Domain.Entities;

namespace BookingService.Application.Common;

public class BookingMappingProfile : Profile
{
    public BookingMappingProfile()
    {
        CreateMap<AvailabilitySlot, SlotResponseDto>();
        CreateMap<Booking, BookingResponseDto>();
    }
}
