using Microsoft.AspNetCore.Mvc;
using StorageWebAppBackend.Models;
using StorageWebAppBackend.Services;

namespace StorageWebAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EditController : ControllerBase
    {
        private readonly DbService _dbService;

        public EditController(DbService dbService)
        {
            _dbService = dbService;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePhoto(string id, [FromBody] PhotoUpdateDto dto)
        {
            Console.WriteLine("EDIT ID: " + id);
            Console.WriteLine("TITLE: " + dto.Title);
            Console.WriteLine("DESC: " + dto.Desc);
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest("UserId is required.");

            if (string.IsNullOrWhiteSpace(dto.Title) && string.IsNullOrWhiteSpace(dto.Desc))
                return BadRequest("Either title or desc must be provided.");

            var photo = await _dbService.GetPhotoByIdAsync(id, dto.UserId);

            if (photo == null)
                return NotFound("Photo not found.");

            if (!string.IsNullOrWhiteSpace(dto.Title))
                photo.title = dto.Title;

            if (!string.IsNullOrWhiteSpace(dto.Desc))
                photo.desc = dto.Desc;

            await _dbService.UpdatePhotoAsync(photo);

            return Ok(photo);
        }
    }
}
