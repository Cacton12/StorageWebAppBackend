namespace StorageWebAppBackend.Services
{
    public class R2Config
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string BucketName { get; set; }
        public string ServiceUrl { get; set; }
        public string ApiToken { get; set; } // optional, can remove now
    }
}
