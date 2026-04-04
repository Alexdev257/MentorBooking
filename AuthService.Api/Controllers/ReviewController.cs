using AuthService.Application.DTOs.Request.Review;
using AuthService.Application.DTOs.Response.Admin;
using AuthService.Application.DTOs.Response.Auth;
using AuthService.Application.Interfaces.Services;
using AuthService.Application.Services;
using AuthService.Domain.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shared.Contracts.Common.Wrappers;
using System.Security.Claims;

namespace AuthService.Api.Controllers
{
    [Route("api/reviews")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        private Guid? GetMenteeIdFromClaim()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("UserId")?.Value
                ?? User.FindFirst("sub")?.Value;
            return Guid.TryParse(userId, out var id) ? id : null;
        }

        private bool IsStudent()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
            return roleClaim != null && roleClaim == ((int)RoleNameEnum.Student).ToString();
        }

        private IActionResult? EnsureStudent(Guid? menteeId)
        {
            if (!IsStudent())
                return StatusCode(StatusCodes.Status403Forbidden, new CommonResponseBase { IsSuccess = false, Message = "Only students can perform this action" });
            if (menteeId == null)
                return Unauthorized(new CommonResponseBase { IsSuccess = false, Message = "Invalid user context" });
            return null;
        }

        private bool IsAdmin()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
            return roleClaim != null && roleClaim == ((int)RoleNameEnum.Admin).ToString();
        }

        private IActionResult? EnsureAdmin(Guid? adminId)
        {
            if (!IsAdmin())
                return StatusCode(StatusCodes.Status403Forbidden, new CommonResponseBase { IsSuccess = false, Message = "Only admin can perform this action" });
            if (adminId == null)
                return Unauthorized(new CommonResponseBase { IsSuccess = false, Message = "Invalid admin context" });
            return null;
        }

        private static void FillValidationErrors<T>(CommonResponse<T> response, ModelStateDictionary modelState)
        {
            response.IsSuccess = false;
            response.Message = "Validation failed";
            foreach (var key in modelState.Keys)
                foreach (var err in modelState[key]!.Errors)
                    response.ListErrors.Add(new Errors { Field = key, Detail = err.ErrorMessage });
        }

        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> GetListReviewAsync([FromQuery] ReviewGetListRequest request)
        {
            if (request == null)
                return BadRequest(new LoginResponse { IsSuccess = false, Message = "Request body is required" });

            if (!ModelState.IsValid)
            {
                var response = new LoginResponse();
                FillValidationErrors(response, ModelState);
                return BadRequest(response);
            }

            var result = await _reviewService.GetListReviewsAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpGet("{mentorId}")]
        public async Task<IActionResult> GetReviewById([FromRoute] Guid mentorId)
        {

            var menteeId = GetMenteeIdFromClaim();
            if (menteeId == null)
            {
                return NotFound("Not found mentee id");
            }
            if (EnsureStudent(menteeId) is { } err)
                return err;

            var result = await _reviewService.GetReviewByIdAsync(mentorId, menteeId.Value);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateReviewAsync([FromBody] ReviewCreateRequest request)
        {
            var menteeId = GetMenteeIdFromClaim();
            if (menteeId == null)
            {
                return NotFound("Not found mentee id");
            }
            if (EnsureStudent(menteeId) is { } err)
                return err;

            var result = await _reviewService.CreateReviewAsync(menteeId.Value, request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReviewAsync([FromRoute] Guid id, [FromBody] ReviewUpdateRequest request)
        {
            var menteeId = GetMenteeIdFromClaim();
            if (menteeId == null)
            {
                return NotFound("Not found mentee id");
            }
            if (EnsureStudent(menteeId) is { } err)
                return err;
            var result = await _reviewService.UpdateReviewAsync(id, request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReviewAsync([FromRoute] Guid id)
        {
            var menteeId = GetMenteeIdFromClaim();
            if (menteeId == null)
            {
                return NotFound("Not found mentee id");
            }
            if (EnsureStudent(menteeId) is { } err)
                return err;
            var result = await _reviewService.DeleteReviewAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
