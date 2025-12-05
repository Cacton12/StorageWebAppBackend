using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using StorageWebAppBackend.Services;
using Amazon.S3;
using Microsoft.Azure.Cosmos;
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

        // POST: api/upload/image
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { error = "User ID is required" });

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            string uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            string uploadedKey = null;
            string signedUrl = null;

            try
            {
                // Upload file to R2 and save metadata in Cosmos DB
                await using (var fileStream = file.OpenReadStream())
                {
                    uploadedKey = await _dbService.UploadPhotoAsync(
                        userId,
                        uniqueFileName,
                        fileStream,
                        file.ContentType
                    );
                }

                signedUrl = _dbService.GetPhotoUrl(uploadedKey);

                return Ok(new
                {
                    message = "Upload successful",
                    key = uploadedKey,
                    url = signedUrl
                });
            }
            catch (AmazonS3Exception s3Ex)
            {
                // R2 / S3-specific error
                Console.WriteLine($"R2 S3 error: {s3Ex.Message}\n{s3Ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "Failed to upload file to R2",
                    details = s3Ex.Message
                });
            }
            catch (CosmosException ce)
            {
                // Cosmos DB-specific error
                Console.WriteLine($"Cosmos DB error: {ce.StatusCode} / {ce.SubStatusCode} - {ce.Message}\n{ce.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "Failed to save photo metadata in Cosmos DB",
                    statusCode = ce.StatusCode,
                    subStatusCode = ce.SubStatusCode,
                    details = ce.Message
                });
            }
            catch (Exception ex)
            {
                // General / unknown errors
                Console.WriteLine($"Unexpected error: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "An unexpected error occurred during upload",
                    details = ex.Message
                });
            }
        }
    }
}
