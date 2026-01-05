using StorageWebAppBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Azure.Cosmos;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// ENVIRONMENT
// ===============================
var isProduction = builder.Environment.IsProduction();

// ===============================
// ENV VARIABLES
// ===============================
string cosmosEndpoint = Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT");
string cosmosKey = Environment.GetEnvironmentVariable("COSMOS_DB_KEY");
string databaseId = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE_ID");
string usersContainerId = Environment.GetEnvironmentVariable("COSMOS_USERS_CONTAINER");
string photosContainerId = Environment.GetEnvironmentVariable("COSMOS_PHOTOS_CONTAINER");
string jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

// ===============================
// PORT (Railway-compatible)
// ===============================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// ===============================
// SERVICES
// ===============================
builder.Services.AddControllers();

// CORS (lock this down later)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ===============================
// COSMOS DB (Conditional)
// ===============================
bool cosmosConfigured =
    !string.IsNullOrWhiteSpace(cosmosEndpoint) &&
    !string.IsNullOrWhiteSpace(cosmosKey) &&
    !string.IsNullOrWhiteSpace(databaseId) &&
    !string.IsNullOrWhiteSpace(usersContainerId) &&
    !string.IsNullOrWhiteSpace(photosContainerId);

if (cosmosConfigured)
{
    Console.WriteLine("[Startup] Cosmos DB configured.");

    var cosmosOptions = new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway,
        MaxRetryAttemptsOnRateLimitedRequests = 5,
        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(5)
    };

    builder.Services.AddSingleton(_ =>
        new DbService(databaseId, photosContainerId, usersContainerId));
}
else
{
    Console.WriteLine("[Startup WARNING] Cosmos DB env vars missing.");

    if (isProduction)
    {
        throw new Exception("Cosmos DB must be configured in production.");
    }
}

// ===============================
// IMAGE SERVICE
// ===============================
builder.Services.AddSingleton<ImageService>();

// ===============================
// JWT AUTH
// ===============================
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (isProduction)
        throw new Exception("JWT_SECRET is required in production.");

    Console.WriteLine("[Startup WARNING] JWT disabled (no JWT_SECRET).");
}
else
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
        });

    builder.Services.AddAuthorization();
}

// ===============================
// PIPELINE
// ===============================
var app = builder.Build();

app.UseRouting();
app.UseCors("AllowFrontend");

if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();

app.Run();
