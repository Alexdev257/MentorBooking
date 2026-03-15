using AuthService.Application.DTOs.Request.Review;
using AuthService.Application.DTOs.Response.Review;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Common.Wrappers;
using Shared.Infrastructure.Extensions;
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

        public async Task<CommonResponse<bool>> DeleteReviewAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(id);
            if (review == null)
            {
                return new CommonResponse<bool>
                {
                    IsSuccess = false,
                    Message = "Review is not exist",
                    Data = false,
                };
            }

            if (review.IsDeleted)
            {
                return new CommonResponse<bool>
                {
                    IsSuccess = false,
                    Message = "Review is deleted",
                    Data = false,
                };
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.Reviews.DeleteAsync(review);
                await _unitOfWork.CommitTransactionAsync();
                return new CommonResponse<bool>
                {
                    IsSuccess = true,
                    Message = "Delete review sucessfully",
                    Data = true,
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CommonResponse<PaginationResponse<object>>> GetListReviewsAsync(ReviewGetListRequest request, CancellationToken cancellationToken = default)
        {
            var reviews = _unitOfWork.Reviews.GetAllAsync()
                                .Include(x => x.Mentor)
                                .Include(x => x.Mentee)
                                .Where(x => !x.IsDeleted)
                                .AsQueryable();

            if(request.BookingId != null && request.BookingId != Guid.Empty)
            {
                reviews = reviews.Where(x => x.BookingId == request.BookingId);
            }
            if(request.MenteeId != null && request.MenteeId != Guid.Empty)
            {
                reviews = reviews.Where(x => x.MenteeId == request.MenteeId);
            }
            if(request.MentorId != null && request.MentorId != Guid.Empty)
            {
                reviews = reviews.Where(x => x.MentorId == request.MentorId);
            }
            if (request.Rating.HasValue)
            {
                reviews = reviews.Where(x => x.Rating ==  request.Rating);
            }
            if (!string.IsNullOrWhiteSpace(request.Comment))
            {
                reviews = reviews.Where(x => x.Comment.ToLower().Contains(request.Comment.ToLower()));
            }
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                switch (request.SortBy.ToLower())
                {
                    case "createdat":
                        reviews = request.Sorting == true
                            ? reviews.OrderByDescending(x => x.CreatedAt)
                            : reviews.OrderBy(x => x.CreatedAt);
                        break;

                    case "rating":
                        reviews = request.Sorting == true
                            ? reviews.OrderByDescending(x => x.Rating)
                            : reviews.OrderBy(x => x.Rating);
                        break;

                    default:
                        reviews = reviews.OrderByDescending(x => x.CreatedAt);
                        break;
                }
            }
            else
            {
                reviews = reviews.OrderByDescending(x => x.CreatedAt);
            }

            var pagedData = await QueryableExtensions.ToPagedListExtensionsAsync(reviews,
                                                                 request.PageNumber,
                                                                 request.PageSize,
                                                                 x => new ReviewResponseDto
                                                                 {
                                                                     Id = x.Id,
                                                                     BookingId = x.BookingId,
                                                                     Comment = x.Comment,
                                                                     Rating = x.Rating,
                                                                     Mentor = new UserDto
                                                                     {
                                                                         Id = x.Mentor.Id,
                                                                         AvatarUrl = x.Mentor.AvatarUrl,
                                                                         Email = x.Mentor.Email,
                                                                         Fullname = x.Mentor.Fullname
                                                                     },
                                                                     Mentee = new UserDto
                                                                     {
                                                                         Id = x.Mentee.Id,
                                                                         AvatarUrl = x.Mentee.AvatarUrl,
                                                                         Email = x.Mentee.Email,
                                                                         Fullname = x.Mentee.Fullname
                                                                     }
                                                                 },
                                                                 request.Fields);
            return new CommonResponse<PaginationResponse<object>>
            {
                IsSuccess = true,
                Message = "Retrieve review successfully",
                Data = pagedData
            };
        }

        public async Task<CommonResponse<ReviewResponseDto?>> GetReviewByIdAsync(Guid mentorId, Guid menteeId, CancellationToken cancellationToken = default)
        {
            var review = await _unitOfWork.Reviews.FindAsync(x => x.MenteeId == menteeId && x.MentorId == mentorId).Include(x => x.Mentor).Include(x => x.Mentee).FirstOrDefaultAsync();
            if (review == null)
            {
                return new CommonResponse<ReviewResponseDto?>
                {
                    IsSuccess = false,
                    Message = "Review is not exist",
                };
            }

            if (review.IsDeleted)
            {
                return new CommonResponse<ReviewResponseDto?>
                {
                    IsSuccess = false,
                    Message = "Review is deleted",
                };
            }

            return new CommonResponse<ReviewResponseDto?>
            {
                IsSuccess = true,
                Message = "Get By Id Sucessfully",
                Data = new ReviewResponseDto
                {
                    Id = review.Id,
                    BookingId = review.BookingId,
                    Comment = review.Comment,
                    Rating = review.Rating,
                    Mentor = new UserDto
                    {
                        Id = review.Mentor.Id,
                        Fullname = review.Mentor.Fullname,
                        AvatarUrl = review.Mentor.AvatarUrl,
                        Email = review.Mentor.Email,
                    },
                    Mentee = new UserDto
                    {
                        Id = review.Mentee.Id,
                        Fullname = review.Mentee.Fullname,
                        AvatarUrl = review.Mentee.AvatarUrl,
                        Email = review.Mentee.Email,
                    }
                }
            };
        }

        public async Task<CommonResponse<ReviewResponseDto>> UpdateReviewAsync(Guid id, ReviewUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var review = await _unitOfWork.Reviews.GetAllAsync().Include(x => x.Mentor).Include(x => x.Mentee).FirstOrDefaultAsync(x => x.Id == id);
            if(review == null)
            {
                return new CommonResponse<ReviewResponseDto>
                {
                    IsSuccess = false,
                    Message = "Review is not found"
                };
            }
            if (review.IsDeleted)
            {
                return new CommonResponse<ReviewResponseDto>
                {
                    IsSuccess = false,
                    Message = "Review is deleted"
                };
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                review.Rating = request.Rating;
                review.Comment = request.Comment;
                await _unitOfWork.CommitTransactionAsync();
                return new CommonResponse<ReviewResponseDto>
                {
                    IsSuccess = true,
                    Message = "Update review Successfully",
                    Data = new ReviewResponseDto
                    {
                        Id = review.Id,
                        BookingId = review.BookingId,
                        Comment = review.Comment,
                        Rating = review.Rating,
                        Mentor = new UserDto
                        {
                            Id = review.Mentor.Id,
                            Fullname = review.Mentor.Fullname,
                            AvatarUrl = review.Mentor.AvatarUrl,
                            Email = review.Mentor.Email
                        },
                        Mentee = new UserDto
                        {
                            Id = review.Mentee.Id,
                            Fullname = review.Mentee.Fullname,
                            Email = review.Mentee.Email,
                            AvatarUrl = review.Mentee.AvatarUrl,
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
