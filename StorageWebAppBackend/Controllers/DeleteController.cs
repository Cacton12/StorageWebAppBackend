using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StorageWebAppBackend.Services;
using System;
using System.Threading.Tasks;

namespace StorageWebAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeleteController : ControllerBase
    {
        private readonly DbService _dbService;

        public DeleteController(DbService dbService)
        {
            _dbService = dbService;
        }

        // DELETE: api/delete?userId=xxx&photoId=yyy&photoKey=zzz
        [HttpDelete]
        public async Task<IActionResult> DeleteImage([FromQuery] string userId, [FromQuery] string photoId, [FromQuery] string photoKey)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(photoId) || string.IsNullOrWhiteSpace(photoKey))
                return BadRequest(new { success = false, error = "userId, photoId, and photoKey are required" });

            try
            {
                await _dbService.DeletePhotoAsync(userId, photoId, photoKey);

                return Ok(new { success = true, message = "Photo deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting photo: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    error = "Failed to delete photo",
                    details = ex.Message
                });
            }
        }
    }
}
