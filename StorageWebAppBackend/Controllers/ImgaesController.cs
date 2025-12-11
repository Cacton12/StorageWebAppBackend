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
                    images = new List<object>()
                });

            try
            {
                var photos = await _dbService.GetUserPhotosAsync(userId, expiresMinutes: 60);

                return Ok(new
                {
                    success = true,
                    message = photos.Count == 0 ? "No images found." : $"Found {photos.Count} image(s).",
                    images = photos
                });
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error retrieving images for user {userId}: {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving images. Please try again later.",
                    images = new List<object>()
                });
            }
        }
    }
}
