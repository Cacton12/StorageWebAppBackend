using StorageWebAppBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Azure.Cosmos;
using System.Text;

// ===============================
// LOAD .ENV FIRST
// ===============================
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ? Allow IConfiguration to read environment variables
builder.Configuration.AddEnvironmentVariables();

// ===============================
// ENV VARIABLES
// ===============================
var isProduction = builder.Environment.IsProduction();

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ===============================
// COSMOS DB
// ===============================
bool cosmosConfigured =
    !string.IsNullOrWhiteSpace(cosmosEndpoint) &&
    !string.IsNullOrWhiteSpace(cosmosKey) &&
    !string.IsNullOrWhiteSpace(databaseId) &&
    !string.IsNullOrWhiteSpace(usersContainerId) &&
    !string.IsNullOrWhiteSpace(photosContainerId);

if (cosmosConfigured)
{
    builder.Services.AddSingleton(new CosmosClient(
        cosmosEndpoint,
        cosmosKey,
        new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway
        }));

    builder.Services.AddSingleton(
        new DbService(databaseId, photosContainerId, usersContainerId));

    Console.WriteLine("? Cosmos DB registered");
}
else
{
    Console.WriteLine("?? Cosmos DB env vars missing");

    if (isProduction)
    {
        throw new Exception("Cosmos DB configuration is required in production.");
    }

    builder.Services.AddSingleton<DbService>(_ =>
        throw new InvalidOperationException(
            "DbService requested but Cosmos is not configured."));
}

// ===============================
// APP SERVICES
// ===============================
builder.Services.AddSingleton<ImageService>();
builder.Services.AddScoped<EmailService>();   // ? Mailgun

// ===============================
// JWT AUTH
// ===============================
if (!string.IsNullOrWhiteSpace(jwtSecret))
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
// BUILD APP
// ===============================
var app = builder.Build();

// ===============================
// PIPELINE
// ===============================
app.UseRouting();
app.UseCors("AllowFrontend");

if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();

app.Run();
