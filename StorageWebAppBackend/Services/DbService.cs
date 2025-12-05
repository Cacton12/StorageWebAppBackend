using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Azure.Cosmos;
using StorageWebAppBackend.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StorageWebAppBackend.Services
{
    public class DbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _usersContainer;
        private readonly Container _photosContainer;

        private readonly IAmazonS3 _s3Client;
        private readonly string _r2BucketName;

        public DbService(string databaseId, string photosContainerId, string usersContainerId)
        {
            var cosmosConfig = LoadCosmosConfig();
            var r2Config = LoadR2Config();

            // Initialize Cosmos DB
            _cosmosClient = new CosmosClient(cosmosConfig.AccountEndpoint, cosmosConfig.AccountKey);
            _photosContainer = _cosmosClient.GetContainer(databaseId, photosContainerId);
            _usersContainer = _cosmosClient.GetContainer(databaseId, usersContainerId);
            Console.WriteLine($"Cosmos DB initialized. DB: {databaseId}, PhotosContainer: {photosContainerId}, UsersContainer: {usersContainerId}");

            // Initialize R2 S3 client
            _r2BucketName = r2Config.BucketName;
            var credentials = new BasicAWSCredentials(r2Config.AccessKey, r2Config.SecretKey);
            _s3Client = new AmazonS3Client(
                credentials,
                new AmazonS3Config
                {
                    ServiceURL = r2Config.ServiceUrl,
                    ForcePathStyle = true,
                    AuthenticationRegion = "auto"
                });
            Console.WriteLine($"R2 S3 client initialized. Bucket: {_r2BucketName}");
        }

        #region Config Loaders
        private CosmosDbConfig LoadCosmosConfig()
        {
            DotNetEnv.Env.Load();
            var endpoint = Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT");
            var key = Environment.GetEnvironmentVariable("COSMOS_DB_KEY");
            Console.WriteLine($"Loaded Cosmos endpoint: {endpoint}, Key length: {key?.Length}");
            return new CosmosDbConfig
            {
                AccountEndpoint = endpoint,
                AccountKey = key
            };
        }

        private R2Config LoadR2Config()
        {
            DotNetEnv.Env.Load();
            return new R2Config
            {
                AccessKey = Environment.GetEnvironmentVariable("R2_ACCESS_KEY"),
                SecretKey = Environment.GetEnvironmentVariable("R2_SECRET_KEY"),
                BucketName = Environment.GetEnvironmentVariable("R2_BUCKET_NAME"),
                ServiceUrl = Environment.GetEnvironmentVariable("R2_SERVICE_URL")
            };
        }
        #endregion

        #region Photo Upload
        public async Task<string> UploadFileToR2Async(string key, Stream fileStream, string contentType)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File stream is null or empty", nameof(fileStream));

            key = Uri.EscapeDataString(key.Replace(" ", "_"));

            try
            {
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await fileStream.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }

                var request = new PutObjectRequest
                {
                    BucketName = _r2BucketName,
                    Key = key,
                    InputStream = new MemoryStream(fileBytes),
                    ContentType = contentType,
                    DisablePayloadSigning = true
                };

                var response = await _s3Client.PutObjectAsync(request);
                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception($"R2 upload failed with status code: {response.HttpStatusCode}");

                Console.WriteLine($"Uploaded file to R2: {key}");
                return key;
            }
            catch (AmazonS3Exception s3Ex)
            {
                Console.WriteLine($"Amazon S3 exception: {s3Ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error uploading to R2: {ex}");
                throw;
            }
        }

        public async Task<string> UploadPhotoAsync(string userId, string fileName, Stream fileStream, string contentType)
        {
            try
            {
                var photoKey = await UploadFileToR2Async(fileName, fileStream, contentType);
                await SavePhotoMetadataAsync(userId, photoKey, fileName);
                return photoKey;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading photo for user {userId}: {ex}");
                throw;
            }
        }

        public string GetPhotoUrl(string key, int expiresInMinutes = 60)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _r2BucketName,
                    Key = key,
                    Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes)
                };

                return _s3Client.GetPreSignedURL(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating pre-signed URL for {key}: {ex}");
                return null;
            }
        }
        #endregion

        #region Metadata & Queries
        public async Task SavePhotoMetadataAsync(string userId, string photoKey, string fileName)
        {
            var photoDoc = new
            {
                id = Guid.NewGuid().ToString(),
                userId,
                photoKey,
                fileName,
                dateCreated = DateTime.UtcNow
            };

            try
            {
                var response = await _photosContainer.CreateItemAsync(photoDoc, new PartitionKey(userId));
                Console.WriteLine($"Saved photo metadata: {photoKey} for user {userId}");
            }
            catch (CosmosException ce)
            {
                Console.WriteLine($"Cosmos DB error while saving photo metadata. StatusCode: {ce.StatusCode}, SubStatus: {ce.SubStatusCode}, Message: {ce.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while saving photo metadata: {ex}");
                throw;
            }
        }

        public async Task<List<string>> GetUserPhotoUrlsAsync(string userId, int expiresInMinutes = 60)
        {
            var urls = new List<string>();
            var query = new QueryDefinition("SELECT c.photoKey FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId);

            var iterator = _photosContainer.GetItemQueryIterator<dynamic>(query);

            try
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        string key = item.photoKey;
                        urls.Add(GetPhotoUrl(key, expiresInMinutes));
                    }
                }
            }
            catch (CosmosException ce)
            {
                Console.WriteLine($"Cosmos DB query error: StatusCode: {ce.StatusCode}, Message: {ce.Message}");
                throw;
            }

            return urls;
        }
        #endregion

        #region User Management
        public async Task<Users> GetUserByEmailAsync(string email)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
                .WithParameter("@email", email);

            var iterator = _usersContainer.GetItemQueryIterator<Users>(query);

            try
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    if (response.Any())
                    {
                        Console.WriteLine($"Found user by email: {email}");
                        return response.First();
                    }
                }
            }
            catch (CosmosException ce)
            {
                Console.WriteLine($"Cosmos DB error querying user: StatusCode: {ce.StatusCode}, Message: {ce.Message}");
                throw;
            }

            Console.WriteLine($"User not found: {email}");
            return null;
        }

        public async Task<Users> CreateUserAsync(Users user)
        {
            try
            {
                var response = await _usersContainer.CreateItemAsync(user, new PartitionKey(user.email));
                Console.WriteLine($"Created user: {user.email}");
                return response.Resource;
            }
            catch (CosmosException ce)
            {
                Console.WriteLine($"Cosmos DB error creating user: StatusCode: {ce.StatusCode}, Message: {ce.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error creating user: {ex}");
                throw;
            }
        }
        #endregion
    }
}
