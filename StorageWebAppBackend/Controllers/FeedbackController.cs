using Microsoft.AspNetCore.Mvc;
using StorageWebAppBackend.Models;
using StorageWebAppBackend.Services;
using System.Threading.Tasks;

namespace StorageWebAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly EmailService _emailService;

        public FeedbackController(EmailService emailService)
        {
            _emailService = emailService;
        }

        // POST: api/feedback
        [HttpPost]
        public async Task<IActionResult> SendFeedback([FromBody] FeedbackRequest request)
        {
            Console.WriteLine("Received feedback request");
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            try
            {
                var result = await _emailService.SendFeedbackEmailAsync(
                    request.Name,
                    request.Email,
                    request.Message
                );

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Feedback sent successfully"
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        error = "Failed to send feedback. Please try again later."
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occurred while sending feedback",
                    details = ex.Message
                });
            }
        }
    }
}