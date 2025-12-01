using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using StorageWebAppBackend.Services;
using System;
using System.Threading.Tasks;

namespace StorageWebAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly DbService _dbService;

        public UploadController(DbService dbService)
        {
            _dbService = dbService;
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { error = "User ID is required" });

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";

            string photoUrl;
            using (var stream = file.OpenReadStream())
            {
                photoUrl = await _dbService.UploadPhotoAsync(userId, fileName, stream, file.ContentType);
            }

            return Ok(new
            {
                message = "Upload successful",
                userId,
                fileName,
                photoUrl
            });
        }
    }
}
