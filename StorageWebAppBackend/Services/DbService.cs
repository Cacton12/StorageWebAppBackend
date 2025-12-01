using Microsoft.Azure.Cosmos;
using StorageWebAppBackend.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace StorageWebAppBackend.Services
{
    public class DbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _usersContainer;
        private readonly Container _photosContainer;

        private readonly HttpClient _httpClient;
        private readonly string _r2BucketName;
        private readonly string _r2ServiceUrl;
        private readonly string _r2ApiToken;

        public DbService(string databaseId, string photosContainerId, string usersContainerId)
        {
            var cosmosConfig = GetCosmosDbConfig();
            _cosmosClient = new CosmosClient(cosmosConfig.AccountEndpoint, cosmosConfig.AccountKey);
            _usersContainer = _cosmosClient.GetContainer(databaseId, usersContainerId);
            _photosContainer = _cosmosClient.GetContainer(databaseId, photosContainerId);

            var r2Config = GetR2Config();
            _r2BucketName = r2Config.BucketName;
            _r2ServiceUrl = r2Config.ServiceUrl;
            _r2ApiToken = r2Config.ApiToken;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _r2ApiToken);
        }

        private CosmosDbConfig GetCosmosDbConfig()
        {
            DotNetEnv.Env.Load();
            return new CosmosDbConfig
            {
                AccountEndpoint = Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT"),
                AccountKey = Environment.GetEnvironmentVariable("COSMOS_DB_KEY")
            };
        }

        private R2Config GetR2Config()
        {
            DotNetEnv.Env.Load();
            return new R2Config
            {
                ApiToken = Environment.GetEnvironmentVariable("R2_API_TOKEN"),
                BucketName = Environment.GetEnvironmentVariable("R2_BUCKET_NAME"),
                ServiceUrl = Environment.GetEnvironmentVariable("R2_SERVICE_URL")
            };
        }

        // Upload to R2
        public async Task<string> UploadFileToR2Async(string key, Stream fileStream, string contentType)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File stream is empty", nameof(fileStream));

            if (!fileStream.CanSeek)
                throw new InvalidOperationException("File stream must be seekable for R2 upload");

            fileStream.Position = 0;
            key = Uri.EscapeDataString(key.Replace(" ", "_"));

            var requestUri = $"{_r2ServiceUrl.TrimEnd('/')}/{_r2BucketName.TrimEnd('/')}/{key}";
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Headers.ContentLength = fileStream.Length;

            var response = await _httpClient.PutAsync(requestUri, content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to upload to R2. Status: {response.StatusCode}. Response: {body}");
            }

            return requestUri;
        }

        // Save metadata to Cosmos
        public async Task SavePhotoMetadataAsync(string userId, string photoUrl, string fileName)
        {
            var photoDoc = new
            {
                id = Guid.NewGuid().ToString(),
                userId,
                photoUrl,
                fileName,
                dateCreated = DateTime.UtcNow
            };

            await _photosContainer.CreateItemAsync(photoDoc, new PartitionKey(userId));
        }

        // Full upload pipeline
        public async Task<string> UploadPhotoAsync(string userId, string fileName, Stream fileStream, string contentType)
        {
            var url = await UploadFileToR2Async(fileName, fileStream, contentType);
            await SavePhotoMetadataAsync(userId, url, fileName);
            return url;
        }

        // Optional: get user by email
        public async Task<Users> GetUserByEmailAsync(string email)
        {
            var queryDef = new QueryDefinition("SELECT * FROM c WHERE c.email = @email").WithParameter("@email", email);
            var iterator = _usersContainer.GetItemQueryIterator<Users>(queryDef);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                if (response.Any()) return response.First();
            }

            return null;
        }

        public async Task<Users> CreateUserAsync(Users user)
        {
            var response = await _usersContainer.CreateItemAsync(user, new PartitionKey(user.email));
            return response.Resource;
        }
    }
}
