using AuthService.Application.DTOs.Request.Admin;
using AuthService.Application.DTOs.Request.Review;
using AuthService.Application.DTOs.Response.Admin;
using AuthService.Application.DTOs.Response.Review;
using Shared.Contracts.Common.Wrappers;
using SharedContracts.Common.Wrappers.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces.Services
{
    public interface IReviewService
    {
        Task<CommonResponse<PaginationResponse<object>>> GetListReviewsAsync(ReviewGetListRequest request, CancellationToken cancellationToken = default);
        Task<CommonResponse<ReviewResponseDto?>> GetReviewByIdAsync(Guid mentorId, Guid menteeId, CancellationToken cancellationToken = default);
        Task<CommonResponse<ReviewResponseDto>> CreateReviewAsync(Guid menteeId, ReviewCreateRequest request, CancellationToken cancellationToken = default);
        Task<CommonResponse<ReviewResponseDto>> UpdateReviewAsync(Guid id, ReviewUpdateRequest request, CancellationToken cancellationToken = default);
        Task<CommonResponse<bool>> DeleteReviewAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
