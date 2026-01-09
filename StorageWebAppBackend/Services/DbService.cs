using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
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
        private readonly Container _photosContainer;
        private readonly Container _usersContainer;

        private readonly IAmazonS3 _s3Client;
        private readonly string _r2BucketName;

        public DbService(string databaseId, string photosContainerId, string usersContainerId)
        {
            var cosmos = LoadCosmosConfig();
            var r2 = LoadR2Config();

            // COSMOS
            _cosmosClient = new CosmosClient(cosmos.AccountEndpoint, cosmos.AccountKey);
            _photosContainer = _cosmosClient.GetContainer(databaseId, photosContainerId);
            _usersContainer = _cosmosClient.GetContainer(databaseId, usersContainerId);

            // R2 (S3 compatible)
            _r2BucketName = r2.BucketName;
            var credentials = new BasicAWSCredentials(r2.AccessKey, r2.SecretKey);

            _s3Client = new AmazonS3Client(
                credentials,
                new AmazonS3Config
                {
                    ServiceURL = r2.ServiceUrl,
                    ForcePathStyle = true,
                    AuthenticationRegion = "auto"
                }
            );
        }

        // ---------------------------------------------------------
        // CONFIG
        // ---------------------------------------------------------

        private CosmosDbConfig LoadCosmosConfig()
        {
            DotNetEnv.Env.Load();

            return new CosmosDbConfig
            {
                AccountEndpoint = Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT"),
                AccountKey = Environment.GetEnvironmentVariable("COSMOS_DB_KEY")
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

        // ---------------------------------------------------------
        // UPLOAD PHOTO
        // ---------------------------------------------------------

        public async Task<PhotoUploadResult> UploadPhotoAsync(
            string userId,
            string fileName,
            Stream fileStream,
            string contentType,
            string title = null,
            string desc = null)
        {
            var photoKey = await UploadFileToR2Async(fileName, fileStream, contentType);

            var photo = new PhotoMetadata
            {
                id = Guid.NewGuid().ToString(),
                userId = userId,
                photoKey = photoKey,
                fileName = fileName,
                title = title ?? fileName,
                desc = desc ?? "Uploaded by user",
                dateCreated = DateTime.UtcNow
            };

            await _photosContainer.CreateItemAsync(photo, new PartitionKey(userId));

            return new PhotoUploadResult
            {
                id = photo.id,
                photoKey = photo.photoKey,
                url = GetPhotoUrl(photo.photoKey),
                title = photo.title,
                desc = photo.desc,
                dateCreated = photo.dateCreated
            };
        }
        public async Task<UserImageUploadResult> UploadUserImageAsync(
            Users user,
            string fileName,
            Stream fileStream,
            bool isProfileImage,
            bool isBannerImage,
            string contentType,
            string title = null,
            string desc = null)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            string? profileKey = null;
            string? bannerKey = null;

            // Upload profile image if requested
            if (isProfileImage)
            {
                profileKey = await UploadFileToR2Async(fileName, fileStream, contentType);
                user.ProfileImage = profileKey;
                await _usersContainer.UpsertItemAsync(user, new PartitionKey(user.email));
            }
            else
            {
                // Use existing profile image if not uploading a new one
                profileKey = user.ProfileImage;
            }

            // Upload banner image if requested
            if (isBannerImage)
            {
                bannerKey = await UploadFileToR2Async(fileName, fileStream, contentType);
                user.Banner = bannerKey;
                await _usersContainer.UpsertItemAsync(user, new PartitionKey(user.email));
            }
            else
            {
                // Use existing banner if not uploading a new one
                bannerKey = user.Banner;
            }

            // Return keys and URLs for whatever was uploaded OR existing values
            return new UserImageUploadResult
            {
                ProfileKey = profileKey,
                ProfileUrl = profileKey != null ? GetPhotoUrl(profileKey) : null,
                BannerKey = bannerKey,
                BannerUrl = bannerKey != null ? GetPhotoUrl(bannerKey) : null,
                DateCreated = DateTime.UtcNow
            };
        }

        public async Task<string> UploadFileToR2Async(string key, Stream stream, string contentType)
        {
            if (stream == null || stream.Length == 0)
                throw new Exception("Empty file stream");

            key = Uri.EscapeDataString(key.Replace(" ", "_"));

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            var request = new PutObjectRequest
            {
                BucketName = _r2BucketName,
                Key = key,
                InputStream = new MemoryStream(ms.ToArray()),
                ContentType = contentType,
                DisablePayloadSigning = true
            };

            var response = await _s3Client.PutObjectAsync(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("R2 upload failed");

            return key;
        }

        public string GetPhotoUrl(string key, int expiresMinutes = 60)
        {
            try
            {
                return _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
                {
                    BucketName = _r2BucketName,
                    Key = key,
                    Expires = DateTime.UtcNow.AddMinutes(expiresMinutes)
                });
            }
            catch { return null; }
        }

        // ---------------------------------------------------------
        // FETCH PHOTOS
        // ---------------------------------------------------------

        public async Task<List<PhotoUploadResult>> GetUserPhotosAsync(string userId, int expiresMinutes = 60)
        {
            var results = new List<PhotoUploadResult>();

            var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @uid")
                .WithParameter("@uid", userId);

            using var iterator = _photosContainer.GetItemQueryIterator<PhotoMetadata>(query);

            while (iterator.HasMoreResults)
            {
                foreach (var photo in await iterator.ReadNextAsync())
                {
                    results.Add(new PhotoUploadResult
                    {
                        id = photo.id,
                        photoKey = photo.photoKey,
                        url = GetPhotoUrl(photo.photoKey, expiresMinutes),
                        title = photo.title,
                        desc = photo.desc,
                        dateCreated = photo.dateCreated
                    });
                }
            }

            return results;
        }

        // ---------------------------------------------------------
        // EDIT / UPDATE PHOTO
        // ---------------------------------------------------------

        public async Task<PhotoMetadata?> GetPhotoByIdAsync(string photoId, string userId)
        {
            try
            {
                var response = await _photosContainer.ReadItemAsync<PhotoMetadata>(
                    photoId,
                    new PartitionKey(userId)
                );
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task UpdatePhotoAsync(PhotoMetadata photo)
        {
            await _photosContainer.ReplaceItemAsync(
                photo,
                photo.id,
                new PartitionKey(photo.userId)
            );
        }

        // ---------------------------------------------------------
        // DELETE PHOTO
        // ---------------------------------------------------------

        public async Task DeletePhotoAsync(string userId, string photoId, string photoKey)
        {
            // delete file
            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _r2BucketName,
                Key = photoKey
            });

            // delete metadata
            await _photosContainer.DeleteItemAsync<PhotoMetadata>(
                photoId,
                new PartitionKey(userId)
            );
        }

        // ---------------------------------------------------------
        // USER MANAGEMENT
        // ---------------------------------------------------------

        public async Task<Users?> GetUserByEmailAsync(string email)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @e")
                .WithParameter("@e", email);

            using var iterator = _usersContainer.GetItemQueryIterator<Users>(query);

            while (iterator.HasMoreResults)
            {
                var batch = await iterator.ReadNextAsync();
                if (batch.Any())
                    return batch.First();
            }

            return null;
        }

        public async Task<Users> CreateUserAsync(Users user)
        {
            var response = await _usersContainer.CreateItemAsync(user, new PartitionKey(user.email));
            return response.Resource;
        }
        public async Task UpdateUserAsync(Users user)
        {
            await _usersContainer.UpsertItemAsync(user, new PartitionKey(user.email));
        }
        public async Task<GetObjectMetadataResponse?> GetKeyFromR2Async(string fileName)
        {
            try
            {
                var request = new Amazon.S3.Model.GetObjectMetadataRequest
                {
                    BucketName = _r2BucketName,
                    Key = Uri.EscapeDataString(fileName.Replace(" ", "_"))
                };

                var response = await _s3Client.GetObjectMetadataAsync(request);
                return response;
            }
            catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // return null if file not found
            }
        }
    }
}
