using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace StorageWebAppBackend.Models
{
    public class Photo
    {
        // Required fields
        [JsonProperty("id")]
        public string Id { get; set; }  // Unique identifier for Cosmos DB

        [JsonProperty("userId")]
        public string UserId { get; set; } // Reference to the user who uploaded

        [JsonProperty("photoUrl")]
        public string PhotoUrl { get; set; } // URL from R2 storage

        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; } // Timestamp of upload

        // Optional metadata fields
        [JsonProperty("filename")]
        public string Filename { get; set; } // Original filename

        [JsonProperty("fileSize")]
        public long FileSize { get; set; } // Size in bytes

        [JsonProperty("fileType")]
        public string FileType { get; set; } // MIME type

        [JsonProperty("width")]
        public int? Width { get; set; } // Optional image width

        [JsonProperty("height")]
        public int? Height { get; set; } // Optional image height

        [JsonProperty("orientation")]
        public string Orientation { get; set; } // "landscape" or "portrait"

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>(); // For search/filtering

        [JsonProperty("description")]
        public string Description { get; set; } // User caption or description

        [JsonProperty("isPublic")]
        public bool IsPublic { get; set; } = true; // Visibility

        [JsonProperty("status")]
        public string Status { get; set; } = "active"; // "active", "deleted", etc.

        [JsonProperty("lastModified")]
        public DateTime? LastModified { get; set; } // Optional last updated timestamp

        [JsonProperty("version")]
        public int? Version { get; set; } // Optional versioning
    }
}
