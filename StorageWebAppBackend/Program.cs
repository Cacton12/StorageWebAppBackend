using StorageWebAppBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load User Secrets (from AppData\Roaming\Microsoft\UserSecrets\<UserSecretsId>\secrets.json)
builder.Configuration.AddUserSecrets<Program>();

// Add controllers
builder.Services.AddControllers();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev",
        policy =>
        {
            policy.WithOrigins("http://localhost:3001") // your React app origin
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Load CosmosDb section
var cosmosSection = builder.Configuration.GetSection("CosmosDb");
string databaseId = cosmosSection["DatabaseId"];
string usersContainerId = cosmosSection["ContainerId"];
string photosContainerId = cosmosSection["ContainerId"];

// Safety check
if (string.IsNullOrWhiteSpace(databaseId) || string.IsNullOrWhiteSpace(usersContainerId))
{
    throw new InvalidOperationException(
        "CosmosDb configuration is missing. Make sure DatabaseId and usersContainerId are set in User Secrets."
    );
}
// Safety check
if (string.IsNullOrWhiteSpace(databaseId) || string.IsNullOrWhiteSpace(photosContainerId))
{
    throw new InvalidOperationException(
        "CosmosDb configuration is missing. Make sure DatabaseId and photosContainerId are set in User Secrets."
    );
}

// Register DbService as singleton
builder.Services.AddSingleton<DbService>(provider =>
    new DbService(databaseId, usersContainerId, photosContainerId));

// --- JWT Configuration ---
string jwtSecret = builder.Configuration["Jwt:Key"]; // Replace with secure key

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, // can be true if you want to restrict issuer
        ValidateAudience = false, // can be true if you want to restrict audience
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();
// --- End JWT Configuration ---

var app = builder.Build();

// Middleware
app.UseRouting();
app.UseCors("AllowReactDev");

// Enable authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
