using StorageWebAppBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;
using Microsoft.Azure.Cosmos;

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

// CORS for local React dev
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
});

// ---- CosmosDB Optimization ----
var cosmosOptions = new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway,        // Or Direct for faster performance
    MaxRetryAttemptsOnRateLimitedRequests = 5,
    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(5)
};

// Register optimized DbService singleton
builder.Services.AddSingleton(provider =>
{
    var client = new CosmosClient(cosmosEndpoint, cosmosKey, cosmosOptions);
    return new DbService(databaseId, photosContainerId, usersContainerId);
});

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

// Lambda hosting without API Gateway
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

app.UseRouting();
app.UseCors("AllowReactDev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
