using Microsoft.AspNetCore.Mvc;
using StorageWebAppBackend.Services;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StorageWebAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly DbService _dbService;

        public ImagesController(DbService dbService)
        {
            _dbService = dbService;
        }

        // GET: api/images/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserImages(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new
                {
                    success = false,
                    message = "User ID is required.",
                    images = new List<string>()
                });

            try
            {
                var urls = await _dbService.GetUserPhotoUrlsAsync(userId, expiresInMinutes: 60);

                if (urls == null || urls.Count == 0)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "No images found for this user.",
                        images = new List<string>()
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"Found {urls.Count} image(s).",
                    images = urls
                });
            }
            catch (System.Exception ex)
            {
                // Log the exception internally (e.g., to file or monitoring system)
                Console.WriteLine($"Error retrieving images for user {userId}: {ex}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving images. Please try again later.",
                    images = new List<string>()
                });
            }
        }
    }
}
