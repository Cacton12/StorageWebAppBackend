using Microsoft.AspNetCore.Mvc;
using StorageWebAppBackend.Models;
using StorageWebAppBackend.Services;
using System;
using System.Threading.Tasks;
using BCrypt.Net;

namespace StorageWebAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignupController : ControllerBase
    {
        private readonly DbService _dbService;

        public SignupController(DbService dbService)
        {
            _dbService = dbService;
        }

        // POST /api/signup
        [HttpPost]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            Console.WriteLine("Signup endpoint hit");

            if (string.IsNullOrEmpty(request.email) || string.IsNullOrEmpty(request.password))
                return BadRequest(new { message = "Email and password are required." });

            // Normalize email to lowercase
            string email = request.email.ToLower();
            string name = request.name;


            // Check if user already exists
            var existingUser = await _dbService.GetUserByEmailAsync(email);

            if (existingUser != null)
                return Conflict(new { message = "Email already registered." });

            // Hash the password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.password);

            // Create new user object
            var newUser = new Users
            {
                id = Guid.NewGuid().ToString(),
                email = email,
                name = name,
                passwordHash = passwordHash,
                dateCreated = DateTime.UtcNow.ToString("o") // ISO 8601 format
            };

            // Save to Cosmos DB
            var createdUser = await _dbService.CreateUserAsync(newUser);

            // Return the created user (without password)
            return Ok(new
            {
                createdUser.id,
                createdUser.email,
                createdUser.name,
                createdUser.dateCreated
            });
        }
    }

    // DTO for signup request
    public class SignUpRequest
    {
        public string email { get; set; }
        public string name { get; set; }
        public string password { get; set; }
    }
}
