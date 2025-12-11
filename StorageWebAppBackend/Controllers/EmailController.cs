using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace StorageWebAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EmailController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("send-feedback")]
        public IActionResult SendFeedback([FromBody] FeedbackRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, error = "All fields are required." });
            }

            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");

                string smtpHost = emailSettings.GetValue<string>("SMTPHost");
                int smtpPort = emailSettings.GetValue<int>("SMTPPort");
                string senderEmail = emailSettings.GetValue<string>("SenderEmail");
                string senderPassword = emailSettings.GetValue<string>("SenderPassword");

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(senderEmail, senderPassword);

                    var mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(senderEmail);
                    mailMessage.To.Add(senderEmail); // send feedback TO yourself
                    mailMessage.Subject = $"Feedback from {request.Name}";
                    mailMessage.Body = $"Name: {request.Name}\nEmail: {request.Email}\n\nMessage:\n{request.Message}";

                    client.Send(mailMessage);
                }

                return Ok(new { success = true, message = "Feedback sent successfully." });
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("EMAIL ERROR: " + ex);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }

    public class FeedbackRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
    }
}
