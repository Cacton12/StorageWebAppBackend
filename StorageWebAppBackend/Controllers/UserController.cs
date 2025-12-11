using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using StorageWebAppBackend.Services;
using StorageWebAppBackend.Models;
using System.Threading.Tasks;

namespace StorageWebAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly DbService _dbService;

        public UserController(DbService dbService)
        {
            _dbService = dbService;
        }

        // PATCH: api/user/update/{email}
        [HttpPatch("update/{email}")]
        public async Task<IActionResult> UpdateUserProfile(
            string email,
            [FromForm] IFormFile? bannerFile,
            [FromForm] IFormFile? profileFile,
            [FromForm] string? name,
            [FromForm] string? removeBanner,
            [FromForm] string? removeProfile) // flags to remove images
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { success = false, error = "User email is required" });

            var user = await _dbService.GetUserByEmailAsync(email);
            if (user == null)
                return NotFound(new { success = false, error = "User not found" });

            // Update or remove banner
            if (bannerFile != null && bannerFile.Length > 0)
            {
                string bannerKey = bannerFile.FileName;

                if (!await _dbService.FileExistsInR2Async(bannerKey))
                {
                    await using var bannerStream = bannerFile.OpenReadStream();
                    bannerKey = await _dbService.UploadFileToR2Async(bannerKey, bannerStream, bannerFile.ContentType);
                }

                user.Banner = _dbService.GetPhotoUrl(bannerKey, 60 * 24 * 7); // 7 days
            }
            else if (!string.IsNullOrEmpty(removeBanner) && removeBanner == "true")
            {
                user.Banner = null; // Remove banner
            }

            // Update or remove profile image
            if (profileFile != null && profileFile.Length > 0)
            {
                string profileKey = profileFile.FileName;

                if (!await _dbService.FileExistsInR2Async(profileKey))
                {
                    await using var profileStream = profileFile.OpenReadStream();
                    profileKey = await _dbService.UploadFileToR2Async(profileKey, profileStream, profileFile.ContentType);
                }

                user.ProfileImage = _dbService.GetPhotoUrl(profileKey, 60 * 24 * 7); // 7 days
            }
            else if (!string.IsNullOrEmpty(removeProfile) && removeProfile == "true")
            {
                user.ProfileImage = null; // Remove profile image
            }

            // Update name if provided
            if (!string.IsNullOrWhiteSpace(name))
            {
                user.name = name;
            }

            await _dbService.UpdateUserAsync(user);

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully",
                user
            });
        }
    }
}
