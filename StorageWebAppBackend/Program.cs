using StorageWebAppBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("Loading .env file...");
Env.Load(); // Loads all variables from .env into Environment.GetEnvironmentVariable()

// --- Load Environment Variables ---
string cosmosEndpoint = Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT");
string cosmosKey = Environment.GetEnvironmentVariable("COSMOS_DB_KEY");
string databaseId = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE_ID");
string usersContainerId = Environment.GetEnvironmentVariable("COSMOS_USERS_CONTAINER");
string photosContainerId = Environment.GetEnvironmentVariable("COSMOS_PHOTOS_CONTAINER");

string jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

// --- Validation ---
if (string.IsNullOrWhiteSpace(cosmosEndpoint) || string.IsNullOrWhiteSpace(cosmosKey))
{
    throw new Exception("Cosmos DB environment variables missing. Check .env file.");
}

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new Exception("JWT_SECRET missing in .env file.");
}

// --- Services ---
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy => policy
        .WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Register Cosmos DB service
builder.Services.AddSingleton(provider =>
    new DbService(databaseId, photosContainerId, usersContainerId)
);

// --- JWT ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

// --- App ---
var app = builder.Build();

app.UseRouting();
app.UseCors("AllowReactDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();