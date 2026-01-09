using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StorageWebAppBackend.Models;
using StorageWebAppBackend.Services;
using System;
using System.Threading.Tasks;

namespace StorageWebAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly DbService _dbService;
        private readonly ImageService _imageService;

        public UserController(DbService dbService, ImageService imageService)
        {
            _dbService = dbService;
            _imageService = imageService;
        }

        // PATCH: api/user/update/{email}
        [HttpPatch("update/{email}")]
        public async Task<IActionResult> UpdateUserProfile(
            string email,
            [FromForm] IFormFile? profileFile,
            [FromForm] IFormFile? bannerFile)
        {
            // 1️⃣ Fetch the user
            var user = await _dbService.GetUserByEmailAsync(email);
            if (user == null)
                return NotFound(new { success = false, error = "User not found" });

            try
            {
                // Generate unique filenames and resize streams if provided
                Stream? profileStream = null;
                Stream? bannerStream = null;
                string? uniqueProfileFile = null;
                string? uniqueBannerFile = null;

                if (profileFile != null)
                {
                    uniqueProfileFile = $"{Guid.NewGuid()}_{profileFile.FileName}";
                    profileStream = await _imageService.ResizeImageAsync(profileFile.OpenReadStream());
                }

                if (bannerFile != null)
                {
                    uniqueBannerFile = $"{Guid.NewGuid()}_{bannerFile.FileName}";
                    bannerStream = await _imageService.ResizeImageAsync(bannerFile.OpenReadStream());
                }

                UserImageUploadResult? profileResult = null;
                UserImageUploadResult? bannerResult = null;

                // 2️⃣ If both files exist, call separately
                if (profileFile != null && bannerFile != null)
                {
                    profileResult = await _dbService.UploadUserImageAsync(
                        user,
                        uniqueProfileFile!,
                        profileStream!,
                        isProfileImage: true,
                        isBannerImage: false,
                        profileFile.ContentType
                    );

                    bannerResult = await _dbService.UploadUserImageAsync(
                        user,
                        uniqueBannerFile!,
                        bannerStream!,
                        isProfileImage: false,
                        isBannerImage: true,
                        bannerFile.ContentType
                    );

                    return Ok(new
                    {
                        success = true,
                        profile = profileResult,
                        banner = bannerResult,
                        user
                    });
                }

                // 3️⃣ If only profile exists
                if (profileFile != null)
                {
                    profileResult = await _dbService.UploadUserImageAsync(
                        user,
                        uniqueProfileFile!,
                        profileStream!,
                        isProfileImage: true,
                        isBannerImage: false,
                        profileFile.ContentType
                    );

                    return Ok(new
                    {
                        success = true,
                        profile = profileResult,
                        user
                    });
                }

                // 4️⃣ If only banner exists
                if (bannerFile != null)
                {
                    bannerResult = await _dbService.UploadUserImageAsync(
                        user,
                        uniqueBannerFile!,
                        bannerStream!,
                        isProfileImage: false,
                        isBannerImage: true,
                        bannerFile.ContentType
                    );

                    return Ok(new
                    {
                        success = true,
                        banner = bannerResult,
                        user
                    });
                }

                // 5️⃣ Nothing uploaded
                return BadRequest(new { success = false, error = "No files uploaded" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}
