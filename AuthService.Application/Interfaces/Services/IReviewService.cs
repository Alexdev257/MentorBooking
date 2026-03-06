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
        Task<CommonResponse<PaginationResponse<ReviewResponseDto>>> GetListReviewsAsync(ReviewGetListRequest request, CancellationToken cancellationToken = default);
        Task<CommonResponse<ReviewResponseDto?>> GetReviewByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<CommonResponse<ReviewResponseDto>> CreateReviewAsync(Guid menteeId, ReviewCreateRequest request, CancellationToken cancellationToken = default);
        Task<CommonResponse<ReviewResponseDto>> UpdateRiewAsync(Guid id, Guid MenteeId, ReviewUpdateRequest request, CancellationToken cancellationToken = default);
        Task<CommonResponse<bool>> DeleteStudentAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
