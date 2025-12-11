using StorageWebAppBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load .env file into environment variables
Console.WriteLine("Loading .env...");
Env.Load();

// ---- Load variables from .env ----
string cosmosEndpoint = Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT");
string cosmosKey = Environment.GetEnvironmentVariable("COSMOS_DB_KEY");
string databaseId = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE_ID");
string usersContainerId = Environment.GetEnvironmentVariable("COSMOS_USERS_CONTAINER");
string photosContainerId = Environment.GetEnvironmentVariable("COSMOS_PHOTOS_CONTAINER");

string jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
Console.WriteLine("JWT_SECRET: " + jwtSecret);


// ---- Validation ----
if (string.IsNullOrWhiteSpace(cosmosEndpoint) ||
    string.IsNullOrWhiteSpace(cosmosKey) ||
    string.IsNullOrWhiteSpace(databaseId) ||
    string.IsNullOrWhiteSpace(usersContainerId) ||
    string.IsNullOrWhiteSpace(photosContainerId))
{
    throw new Exception("Missing one or more Cosmos DB environment variables. Check your .env file.");
}

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new Exception("Missing JWT_SECRET in .env file.");
}

// ---- Register Services ----
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

// Cosmos DB service
builder.Services.AddSingleton(provider =>
    new DbService(databaseId, photosContainerId, usersContainerId)
);

// Image service
builder.Services.AddSingleton<ImageService>();

// ---- JWT Auth Setup ----
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

var app = builder.Build();

app.UseRouting();
app.UseCors("AllowReactDev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
