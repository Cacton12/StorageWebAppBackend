namespace StorageWebAppBackend.Models
{
    public class UserImageUploadResult
    {
        public string? ProfileKey { get; set; }
        public string? ProfileUrl { get; set; }

        public string? BannerKey { get; set; }
        public string? BannerUrl { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }

}
