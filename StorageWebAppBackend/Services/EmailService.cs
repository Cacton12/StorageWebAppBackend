using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace StorageWebAppBackend.Services
{
    public class EmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _toEmail;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration configuration)
        {
            _apiKey = configuration["MAILERSEND_API_KEY"]
                ?? throw new ArgumentNullException("MAILERSEND_API_KEY");

            // Use your verified test domain for from.email
            _fromEmail = configuration["MAILERSEND_FROM_EMAIL"]
                ?? "feedback@test-r6ke4n1e029gon12.mlsender.net";

            _toEmail = configuration["MAILERSEND_TO_EMAIL"]
                ?? throw new ArgumentNullException("MAILERSEND_TO_EMAIL");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.mailersend.com/v1/email")
            };
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<bool> SendFeedbackEmailAsync(string senderName, string senderEmail, string message)
        {
            try
            {
                var emailBody = $@"
                <html>
                <body style=""font-family: Arial, sans-serif; background-color: #f9f9f9; padding: 20px;"">
                    <div style=""max-width: 600px; margin: auto; background-color: #ffffff; border-radius: 8px; padding: 20px; box-shadow: 0 0 10px rgba(0,0,0,0.1);"">
                        <h2 style=""color: #51803e; text-align: center;"">New Feedback Received</h2>
                        <hr style=""border: 0; border-top: 1px solid #e0e0e0; margin: 20px 0;"" />
                        <p><strong>Name:</strong> {senderName}</p>
                        <p><strong>Email:</strong> {senderEmail}</p>
                        <p><strong>Message:</strong></p>
                        <div style=""background-color: #f3f4f6; padding: 15px; border-radius: 5px; margin-top: 10px;"">
                            {message.Replace("\n", "<br>")}
                        </div>
                        <hr style=""border: 0; border-top: 1px solid #e0e0e0; margin: 20px 0;"" />
                        <p style=""text-align: center; color: #9ca3af; font-size: 12px;"">
                            This email was sent from StorageWebApp.
                        </p>
                    </div>
                </body>
                </html>";

                var payload = new
                {
                    from = new { email = _fromEmail, name = "StorageWebApp Feedback" },
                    to = new[] { new { email = _toEmail } },
                    subject = $"New Feedback from {senderName}",
                    html = emailBody
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Email sent successfully!");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to send email: {response.StatusCode} - {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                return false;
            }
        }
    }
}
