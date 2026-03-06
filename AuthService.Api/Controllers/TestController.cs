using AuthService.Infrastructure.Implements.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Interfaces;

namespace AuthService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IStorageService _storageService;
        public TestController(IStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            var fileName = $"avatars/{Guid.NewGuid()}_{file.FileName}";

            using var stream = file.OpenReadStream();

            var url = await _storageService.UploadFileAsync(
                fileName,
                stream);

            return Ok(new { url });
        }

        [HttpDelete("delete-by-url")]
        public async Task<IActionResult> DeleteByUrl([FromQuery] string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return BadRequest("File URL is required.");

            await _storageService.DeleteFileFromUrlAsync(fileUrl);

            return Ok(new
            {
                message = "File deleted successfully"
            });
        }
    }
}
