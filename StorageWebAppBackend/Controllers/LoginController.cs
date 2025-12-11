using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StorageWebAppBackend.Models;
using StorageWebAppBackend.Services;
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

        public LoginController(DbService dbService, IConfiguration config)
        {
            _dbService = dbService;
            _jwtSecret = config["Jwt:Key"]; // LOADED FROM USER SECRETS
        }

        // POST: /api/login
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.email) || string.IsNullOrEmpty(request.password))
                return BadRequest(new { message = "Email and password are required." });

            string email = request.email.ToLower();

            // Get user by email
            var user = await _dbService.GetUserByEmailAsync(email);

            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            // Validate password
            bool passwordMatch = BCrypt.Net.BCrypt.Verify(request.password, user.passwordHash);

            if (!passwordMatch)
                return Unauthorized(new { message = "Invalid email or password." });

            // Generate JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("email", user.email),
                    new Claim("id", user.id)
                }),
                Expires = DateTime.UtcNow.AddHours(6),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            string jwt = tokenHandler.WriteToken(token);

            // Return token + user info
            return Ok(new
            {
                token = jwt,
                user = new
                {
                    user.id,
                    user.email,
                    user.name,
                    user.Banner,
                    user.ProfileImage
                }
            });
        }
    }

    public class LoginRequest
    {
        public string email { get; set; }
        public string password { get; set; }
    }
}
