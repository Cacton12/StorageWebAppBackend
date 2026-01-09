using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace StorageWebAppBackend.Services
{
    public class EmailService
    {
        private readonly string _fromEmail;
        private readonly string _password;
        private readonly string _toEmail;

        public EmailService(IConfiguration configuration)
        {
            _fromEmail = configuration["GMAIL_EMAIL"]
                ?? throw new ArgumentNullException("GMAIL_EMAIL");
            _password = configuration["GMAIL_PASSWORD"]
                ?? throw new ArgumentNullException("GMAIL_PASSWORD");
            _toEmail = configuration["GMAIL_TO_EMAIL"]
                ?? throw new ArgumentNullException("GMAIL_TO_EMAIL");
        }

        public async Task<bool> SendFeedbackEmailAsync(string senderName, string senderEmail, string message)
        {
            try
            {
                var mail = new MailMessage();
                mail.From = new MailAddress(_fromEmail, "StorageWebApp Feedback");
                mail.To.Add(_toEmail);
                mail.Subject = $"New Feedback from {senderName}";
                mail.IsBodyHtml = true;

                mail.Body = $@"
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

                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(_fromEmail, _password),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(mail);
                Console.WriteLine("Email sent successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                return false;
            }
        }
    }
}
