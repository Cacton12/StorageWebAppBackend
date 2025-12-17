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
        private readonly ImageService _imageService;

        public UploadController(DbService dbService, ImageService imageService)
        {
            _dbService = dbService;
            _imageService = imageService;
        }



        // POST: api/upload/image
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string userId, [FromForm] string title = null, [FromForm] string desc = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { success = false, error = "User ID is required" });

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, error = "No file uploaded" });

            try
            {
                string uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";

                await using var resizedStream = await _imageService.ResizeImageAsync(file.OpenReadStream());
                var result = await _dbService.UploadPhotoAsync(
                    userId,
                    uniqueFileName,
                    resizedStream,
                    file.ContentType,
                    title,
                    desc
                );


                return Ok(new
                {
                    success = true,
                    message = "Upload successful",
                    photo = result
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    error = "An error occurred during upload",
                    details = ex.Message
                });
            }
        }
    }
}
