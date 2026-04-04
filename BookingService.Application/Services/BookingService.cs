using BookingService.Application.DTOs.Request;
using BookingService.Application.DTOs.Response;
using BookingService.Application.Interfaces.Helpers;
using BookingService.Application.Interfaces.Repositories;
using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Entities;
using BookingService.Domain.Enum;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Common.Wrappers;
using SharedContracts.Common.Wrappers.Requests;
using Shared.Contracts.Interfaces;
using Shared.Contracts.Events;

namespace BookingService.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingUnitOfWork _unitOfWork;
    private readonly IQueryablePager _pager;
    private readonly IMapper _mapper;
    private readonly IZoomService _zoomService;
    private readonly IMessageProducer _messageProducer;
    private readonly IUserService _userService;

    public BookingService(IBookingUnitOfWork unitOfWork, IQueryablePager pager, IMapper mapper, IZoomService zoomService, IMessageProducer messageProducer, IUserService userService)
    {
        _unitOfWork = unitOfWork;
        _pager = pager;
        _mapper = mapper;
        _zoomService = zoomService;
        _messageProducer = messageProducer;
        _userService = userService;
    }

    public async Task<CommonResponse<List<SlotResponseDto>>> GetAvailableSlotsAsync(Guid mentorId, DateTime? from, DateTime? to, bool includeBooked = false, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.AvailabilitySlots.FindAsync(s => s.MentorId == mentorId && !s.IsDeleted);
        if (!includeBooked)
            query = query.Where(s => !s.IsBooked);
        if (from.HasValue)
            query = query.Where(s => s.EndAt > from.Value);
        if (to.HasValue)
            query = query.Where(s => s.StartAt < to.Value);
        var slots = await query.OrderBy(s => s.StartAt).ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<SlotResponseDto>>(slots);
        return new CommonResponse<List<SlotResponseDto>> { IsSuccess = true, Message = "Success", Data = dtos };
    }

    public async Task<CommonResponse<SlotResponseDto?>> GetSlotByIdAsync(Guid mentorId, Guid slotId, CancellationToken cancellationToken = default)
    {
        var slot = await _unitOfWork.AvailabilitySlots.FindAsync(s => s.Id == slotId && s.MentorId == mentorId).FirstOrDefaultAsync(cancellationToken);
        if (slot == null)
            return new CommonResponse<SlotResponseDto?> { IsSuccess = false, Message = "Slot not found", Data = null };
        return new CommonResponse<SlotResponseDto?> { IsSuccess = true, Message = "Success", Data = _mapper.Map<SlotResponseDto>(slot) };
    }

    public async Task<CommonResponse<SlotResponseDto>> CreateSlotAsync(Guid mentorId, CreateSlotRequest request, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<SlotResponseDto> { IsSuccess = false };
        if (request.EndAt <= request.StartAt)
        {
            response.Message = "EndAt must be after StartAt";
            return response;
        }
        var slot = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            MentorId = mentorId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            IsBooked = false
        };
        await _unitOfWork.AvailabilitySlots.AddAsync(slot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Slot created successfully";
        response.Data = _mapper.Map<SlotResponseDto>(slot);
        return response;
    }

    public async Task<CommonResponse<SlotResponseDto>> UpdateSlotAsync(Guid mentorId, Guid slotId, UpdateSlotRequest request, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<SlotResponseDto> { IsSuccess = false };
        var slot = await _unitOfWork.AvailabilitySlots.FindAsync(s => s.Id == slotId && s.MentorId == mentorId).FirstOrDefaultAsync(cancellationToken);
        if (slot == null)
        {
            response.Message = "Slot not found";
            return response;
        }
        if (slot.IsBooked)
        {
            response.Message = "Cannot update a booked slot";
            return response;
        }
        if (request.EndAt <= request.StartAt)
        {
            response.Message = "EndAt must be after StartAt";
            return response;
        }
        slot.StartAt = request.StartAt;
        slot.EndAt = request.EndAt;
        _unitOfWork.AvailabilitySlots.UpdateAsync(slot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Slot updated successfully";
        response.Data = _mapper.Map<SlotResponseDto>(slot);
        return response;
    }

    public async Task<CommonResponse<bool>> DeleteSlotAsync(Guid mentorId, Guid slotId, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<bool> { IsSuccess = false, Data = false };
        var slot = await _unitOfWork.AvailabilitySlots.GetByIdAsync(slotId);
        if (slot == null || slot.MentorId != mentorId)
        {
            response.Message = "Slot not found";
            return response;
        }
        if (slot.IsBooked)
        {
            response.Message = "Cannot delete a booked slot";
            return response;
        }
        _unitOfWork.AvailabilitySlots.DeleteAsync(slot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Slot deleted successfully";
        response.Data = true;
        return response;
    }

    public async Task<CommonResponse<BookingResponseDto>> CreateBookingAsync(Guid menteeId, CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<BookingResponseDto> { IsSuccess = false };
        var slot = await _unitOfWork.AvailabilitySlots.GetByIdAsync(request.SlotId);
        if (slot == null)
        {
            response.Message = "Slot not found";
            return response;
        }
        if (slot.MentorId != request.MentorId)
        {
            response.Message = "Slot does not belong to this mentor";
            return response;
        }
        if (slot.IsBooked)
        {
            response.Message = "Slot is already booked";
            return response;
        }
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            MentorId = request.MentorId,
            MenteeId = menteeId,
            SlotId = request.SlotId,
            Status = (int)BookingStatusEnum.Pending,
            Topic = request.Topic,
            Notes = request.Notes,
            PriceAmount = request.PriceAmount,
            Currency = request.Currency ?? "VND",
            ScheduleStart = slot.StartAt,
            ScheduleEnd = slot.EndAt
        };

        // Fetch mentee info from AuthService
        var menteeInfo = await _userService.GetUserInfoAsync(menteeId, cancellationToken);
        var menteeEmail = menteeInfo?.Email ?? "unknown@example.com";
        var menteeName = menteeInfo?.FullName ?? "Mentee";

        // Add primary mentee as a participant
        booking.Participants.Add(new BookingParticipant
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            MenteeId = menteeId,
            Email = menteeEmail,
            Name = menteeName
        });

        // Add invited mentees
        foreach (var email in request.InvitedEmails)
        {
            booking.Participants.Add(new BookingParticipant
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Email = email,
                Name = "Invited Guest"
            });
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Bookings.AddAsync(booking);
            slot.IsBooked = true;
            _unitOfWork.AvailabilitySlots.UpdateAsync(slot);
            await _unitOfWork.CommitTransactionAsync();
            response.IsSuccess = true;
            response.Message = "Booking created successfully. Waiting for mentor acceptance.";
            response.Data = _mapper.Map<BookingResponseDto>(booking);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<CommonResponse<BookingResponseDto?>> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null)
            return new CommonResponse<BookingResponseDto?> { IsSuccess = false, Message = "Booking not found", Data = null };
        return new CommonResponse<BookingResponseDto?> { IsSuccess = true, Message = "Success", Data = _mapper.Map<BookingResponseDto>(booking) };
    }

    public async Task<CommonResponse<PaginationResponse<BookingResponseDto>>> GetMenteeBookingsAsync(Guid menteeId, PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Bookings.FindAsync(b => b.MenteeId == menteeId).OrderByDescending(b => b.CreatedAt);
        var paged = await _pager.ToPagedListAsync(query, request.PageNumber, request.PageSize, cancellationToken);
        var dtos = _mapper.Map<List<BookingResponseDto>>(paged.Items);
        var result = new PaginationResponse<BookingResponseDto>
        {
            Items = dtos,
            TotalItems = paged.TotalItems,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        };
        return new CommonResponse<PaginationResponse<BookingResponseDto>> { IsSuccess = true, Message = "Success", Data = result };
    }

    public async Task<CommonResponse<PaginationResponse<BookingResponseDto>>> GetMentorBookingsAsync(Guid mentorId, PaginationRequest request, int? status, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Bookings.FindAsync(b => b.MentorId == mentorId);
        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);
        query = query.OrderByDescending(b => b.CreatedAt);
        var paged = await _pager.ToPagedListAsync(query, request.PageNumber, request.PageSize, cancellationToken);
        var dtos = _mapper.Map<List<BookingResponseDto>>(paged.Items);
        var result = new PaginationResponse<BookingResponseDto>
        {
            Items = dtos,
            TotalItems = paged.TotalItems,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        };
        return new CommonResponse<PaginationResponse<BookingResponseDto>> { IsSuccess = true, Message = "Success", Data = result };
    }

    public async Task<CommonResponse<BookingResponseDto>> AcceptBookingAsync(Guid bookingId, Guid mentorId, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<BookingResponseDto> { IsSuccess = false };
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null)
        {
            response.Message = "Booking not found";
            return response;
        }
        if (booking.MentorId != mentorId)
        {
            response.Message = "You are not the mentor of this booking";
            return response;
        }
        if (booking.Status != (int)BookingStatusEnum.Pending)
        {
            response.Message = "Only pending bookings can be accepted";
            return response;
        }
        booking.Status = (int)BookingStatusEnum.Confirmed;

        // Fetch mentor info from AuthService
        var mentorInfo = await _userService.GetUserInfoAsync(booking.MentorId, cancellationToken);
        var mentorEmail = mentorInfo?.Email ?? "mentor@unknown.com";
        var mentorName = mentorInfo?.FullName ?? "Mentor";

        var (meetingId, joinUrlCommon, hostUrl) = await _zoomService.CreateMeetingAsync(booking, cancellationToken);
        if (!string.IsNullOrEmpty(meetingId))
            booking.GoogleEventId = meetingId; // Reusing field for Zoom Meeting Id
        
        // Register all participants
        var participants = await _unitOfWork.BookingParticipants.FindAsync(p => p.BookingId == booking.Id).ToListAsync(cancellationToken);
        foreach (var participant in participants)
        {
            var joinUrl = await _zoomService.AddRegistrantAsync(meetingId!, participant.Email, participant.Name, null, cancellationToken);
            if (!string.IsNullOrEmpty(joinUrl))
            {
                participant.ZoomJoinUrl = joinUrl;
                _unitOfWork.BookingParticipants.UpdateAsync(participant);

                // Publish email invitation event for Mentee
                await _messageProducer.PublishAsync(new SendZoomInviteEvent(
                    participant.Email,
                    participant.Name,
                    mentorName,
                    booking.Topic ?? "Mentor Session",
                    joinUrl,
                    booking.ScheduleStart.AddHours(7),
                    booking.ScheduleEnd.AddHours(7)
                ), cancellationToken);
            }
        }

        if (!string.IsNullOrEmpty(hostUrl))
        {
            booking.MeetingLink = hostUrl; // Store Host URL for Mentor
            
            // Publish host invitation event for Mentor
            await _messageProducer.PublishAsync(new SendZoomHostInviteEvent(
                mentorEmail,
                mentorName,
                booking.Topic ?? "Mentor Session",
                hostUrl,
                booking.ScheduleStart.AddHours(7),
                booking.ScheduleEnd.AddHours(7)
            ), cancellationToken);
        }

        _unitOfWork.Bookings.UpdateAsync(booking);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(joinUrlCommon))
        {
            await _messageProducer.PublishAsync(new BookingAcceptedEvent(
                booking.Id,
                booking.MentorId,
                booking.MenteeId,
                joinUrlCommon,
                booking.ScheduleStart,
                booking.ScheduleEnd
            ), cancellationToken);
        }
        response.IsSuccess = true;
        response.Message = !string.IsNullOrEmpty(hostUrl)
            ? "Booking accepted. Meeting link has been created."
            : "Booking accepted. Meeting link will be sent to the student.";
        response.Data = _mapper.Map<BookingResponseDto>(booking);
        return response;
    }

    public async Task<CommonResponse<BookingResponseDto>> RejectBookingAsync(Guid bookingId, Guid mentorId, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<BookingResponseDto> { IsSuccess = false };
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null)
        {
            response.Message = "Booking not found";
            return response;
        }
        if (booking.MentorId != mentorId)
        {
            response.Message = "You are not the mentor of this booking";
            return response;
        }
        if (booking.Status != (int)BookingStatusEnum.Pending)
        {
            response.Message = "Only pending bookings can be rejected";
            return response;
        }
        booking.Status = (int)BookingStatusEnum.Rejected;
        _unitOfWork.Bookings.UpdateAsync(booking);
        if (booking.SlotId.HasValue)
        {
            var slot = await _unitOfWork.AvailabilitySlots.GetByIdAsync(booking.SlotId.Value);
            if (slot != null)
            {
                slot.IsBooked = false;
                _unitOfWork.AvailabilitySlots.UpdateAsync(slot);
            }
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Booking rejected";
        response.Data = _mapper.Map<BookingResponseDto>(booking);
        return response;
    }

    public async Task<CommonResponse<BookingResponseDto>> CancelBookingAsync(Guid bookingId, Guid userId, bool isMentor, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<BookingResponseDto> { IsSuccess = false };
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null)
        {
            response.Message = "Booking not found";
            return response;
        }
        if (isMentor && booking.MentorId != userId)
        {
            response.Message = "You are not the mentor of this booking";
            return response;
        }
        if (!isMentor && booking.MenteeId != userId)
        {
            response.Message = "You are not the mentee of this booking";
            return response;
        }
        if (booking.Status == (int)BookingStatusEnum.Cancelled)
        {
            response.Message = "Booking is already cancelled";
            return response;
        }
        if (booking.Status == (int)BookingStatusEnum.Completed)
        {
            response.Message = "Cannot cancel a completed booking";
            return response;
        }
        booking.Status = (int)BookingStatusEnum.Cancelled;
        _unitOfWork.Bookings.UpdateAsync(booking);
        if (booking.SlotId.HasValue)
        {
            var slot = await _unitOfWork.AvailabilitySlots.GetByIdAsync(booking.SlotId.Value);
            if (slot != null)
            {
                slot.IsBooked = false;
                _unitOfWork.AvailabilitySlots.UpdateAsync(slot);
            }
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Booking cancelled";
        response.Data = _mapper.Map<BookingResponseDto>(booking);
        return response;
    }
}
