using AuthService.Application.DTOs.Request.Review;
using AuthService.Application.DTOs.Response.Review;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Common.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IAuthUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public ReviewService(IAuthUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CommonResponse<ReviewResponseDto>> CreateReviewAsync(Guid menteeId, ReviewCreateRequest request, CancellationToken cancellationToken = default)
        {
            var checkDup = await _unitOfWork.Reviews
                    .FindAsync(x => x.MenteeId == menteeId && x.BookingId == request.BookingId)
                    .FirstOrDefaultAsync();
            if (checkDup != null)
            {
                return new CommonResponse<ReviewResponseDto>
                {
                    IsSuccess = false,
                    Message = "Mentee reviewed already"
                };
            }
            if(request.Rating < 1 || request.Rating > 5)
            {
                return new CommonResponse<ReviewResponseDto>
                {
                    IsSuccess = false,
                    Message = "Rating must be in range from 1 to 5"
                };
            }

            var review = new Review
            {
                Id = Guid.NewGuid(),
                BookingId = request.BookingId,
                MenteeId = menteeId,
                MentorId = request.MentorId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow,
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Reviews.AddAsync(review);
                await _unitOfWork.CommitTransactionAsync();
                var reviewWithUsers = await _unitOfWork.Reviews
                    .FindAsync(x => x.Id == review.Id)
                    .Include(x => x.Mentor)
                    .Include(x => x.Mentee)
                    .FirstOrDefaultAsync(cancellationToken);

                return new CommonResponse<ReviewResponseDto>
                {
                    IsSuccess = true,
                    Message = "Create Review Successfully",
                    Data = _mapper.Map<ReviewResponseDto>(reviewWithUsers)
                };
            }
            catch(Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public Task<CommonResponse<bool>> DeleteStudentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CommonResponse<PaginationResponse<ReviewResponseDto>>> GetListReviewsAsync(ReviewGetListRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CommonResponse<ReviewResponseDto?>> GetReviewByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CommonResponse<ReviewResponseDto>> UpdateRiewAsync(Guid id, Guid MenteeId, ReviewUpdateRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
