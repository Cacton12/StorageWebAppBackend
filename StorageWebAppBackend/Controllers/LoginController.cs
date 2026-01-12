using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using StorageWebAppBackend.Models;
using StorageWebAppBackend.Services;
using StorageWebAppBackend.Middleware;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;

namespace StorageWebAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly DbService _dbService;
        private readonly string _jwtSecret;
        private readonly ILogger<LoginController> _logger;
        private static readonly TimeSpan TokenExpiration = TimeSpan.FromHours(6);

        public LoginController(DbService dbService, ILogger<LoginController> logger)
        {
            _dbService = dbService;
            _logger = logger;
            _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

            if (string.IsNullOrWhiteSpace(_jwtSecret))
            {
                _logger.LogCritical("JWT_SECRET is not set in the environment");
                throw new InvalidOperationException("JWT_SECRET is not set in the environment or .env file.");
            }
        }

        /// <summary>
        /// Authenticate user and generate JWT token
        /// POST: /api/login
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Validate request
            if (request == null)
            {
                _logger.LogWarning("Login attempt with null request body");
                return BadRequest(new { message = "Request body is required." });
            }

            if (string.IsNullOrWhiteSpace(request.email))
            {
                _logger.LogWarning("Login attempt with missing email");
                return BadRequest(new { message = "Email is required.", code = "MISSING_EMAIL" });
            }

            if (string.IsNullOrWhiteSpace(request.password))
            {
                _logger.LogWarning("Login attempt with missing password. Email: {Email}", request.email);
                return BadRequest(new { message = "Password is required.", code = "MISSING_PASSWORD" });
            }

            // Validate email format
            if (!IsValidEmail(request.email))
            {
                _logger.LogWarning("Login attempt with invalid email format. Email: {Email}", request.email);
                return BadRequest(new { message = "Invalid email format.", code = "INVALID_EMAIL" });
            }

            string email = request.email.ToLower().Trim();

            try
            {
                // Get user by email
                var user = await _dbService.GetUserByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt for non-existent user. Email: {Email}", email);
                    // Use generic message to avoid user enumeration
                    return Unauthorized(new { message = "Invalid email or password.", code = "INVALID_CREDENTIALS" });
                }

                // Validate password
                bool passwordMatch;
                try
                {
                    passwordMatch = BCrypt.Net.BCrypt.Verify(request.password, user.passwordHash);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verifying password hash for user. Email: {Email}", email);
                    return StatusCode(500, new { message = "Authentication error occurred.", code = "AUTH_ERROR" });
                }

                if (!passwordMatch)
                {
                    _logger.LogWarning("Failed login attempt - invalid password. Email: {Email}", email);
                    return Unauthorized(new { message = "Invalid email or password.", code = "INVALID_CREDENTIALS" });
                }

                // Generate JWT
                string jwt;
                try
                {
                    jwt = GenerateJwtToken(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating JWT token. Email: {Email}", email);
                    return StatusCode(500, new { message = "Error generating authentication token.", code = "TOKEN_ERROR" });
                }

                // Generate signed URLs using DbService
                string profileUrl = null;
                string bannerUrl = null;

                try
                {
                    if (!string.IsNullOrEmpty(user.ProfileImage))
                    {
                        profileUrl = _dbService.GetPhotoUrl(user.ProfileImage);
                    }

                    if (!string.IsNullOrEmpty(user.Banner))
                    {
                        bannerUrl = _dbService.GetPhotoUrl(user.Banner);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error generating pre-signed URLs for user images. Email: {Email}", email);
                    // Continue without images - non-critical error
                }

                _logger.LogInformation("Successful login. Email: {Email}, UserId: {UserId}", email, user.id);

                // Return success response
                return Ok(new
                {
                    token = jwt,
                    expiresIn = TokenExpiration.TotalSeconds,
                    user = new
                    {
                        user.id,
                        user.email,
                        user.name,
                        profileImage = profileUrl,
                        banner = bannerUrl
                    }
                });
            }
            catch (AppUnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized login attempt. Email: {Email}", email);
                return Unauthorized(new { message = ex.Message, code = "UNAUTHORIZED" });
            }
            catch (AppValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during login. Email: {Email}", email);
                return BadRequest(new { message = ex.Message, code = "VALIDATION_ERROR" });
            }
            catch (Microsoft.Azure.Cosmos.CosmosException ex)
            {
                _logger.LogError(ex, "Database error during login. Email: {Email}", email);
                return StatusCode(503, new { message = "Database service temporarily unavailable.", code = "DATABASE_ERROR" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login. Email: {Email}", email);
                return StatusCode(500, new { message = "An unexpected error occurred.", code = "INTERNAL_ERROR" });
            }
        }

        /// <summary>
        /// Generate JWT token for authenticated user
        /// </summary>
        private string GenerateJwtToken(Users user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("email", user.email),
                    new Claim("id", user.id),
                    new Claim("name", user.name ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) // Issued at
                }),
                Expires = DateTime.UtcNow.Add(TokenExpiration),
                Issuer = "StorageWebApp",
                Audience = "StorageWebApp",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Validate email format
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Health check endpoint for login service
        /// GET: /api/login/health
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "LoginController"
            });
        }
    }

    /// <summary>
    /// Login request model
    /// </summary>
    public class LoginRequest
    {
        public string email { get; set; }
        public string password { get; set; }
    }
}