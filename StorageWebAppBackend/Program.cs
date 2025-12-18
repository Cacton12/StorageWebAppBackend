using StorageWebAppBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// ENVIRONMENT VARIABLES (Railway)
// ===============================
string cosmosEndpoint = Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT");
string cosmosKey = Environment.GetEnvironmentVariable("COSMOS_DB_KEY");
string databaseId = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE_ID");
string usersContainerId = Environment.GetEnvironmentVariable("COSMOS_USERS_CONTAINER");
string photosContainerId = Environment.GetEnvironmentVariable("COSMOS_PHOTOS_CONTAINER");
string jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

// ===============================
// VALIDATION
// ===============================
if (string.IsNullOrWhiteSpace(cosmosEndpoint) ||
    string.IsNullOrWhiteSpace(cosmosKey) ||
    string.IsNullOrWhiteSpace(databaseId) ||
    string.IsNullOrWhiteSpace(usersContainerId) ||
    string.IsNullOrWhiteSpace(photosContainerId))
{
    throw new Exception("Missing Cosmos DB environment variables.");
}

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new Exception("Missing JWT_SECRET environment variable.");
}

// ===============================
// KESTREL PORT (Railway)
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

// CORS (update origin later for production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

// ===============================
// COSMOS DB
// ===============================
var cosmosOptions = new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway,
    MaxRetryAttemptsOnRateLimitedRequests = 5,
    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(5)
};

builder.Services.AddSingleton(provider =>
{
    var client = new CosmosClient(cosmosEndpoint, cosmosKey, cosmosOptions);
    return new DbService(databaseId, photosContainerId, usersContainerId);
});

builder.Services.AddSingleton<ImageService>();

// ===============================
// JWT AUTH
// ===============================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret)
        )
    };
});

builder.Services.AddAuthorization();

// ===============================
// APP PIPELINE
// ===============================
var app = builder.Build();

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
