namespace StorageWebAppBackend.Models
{
    public class PhotoUploadResult
    {
        public string id { get; set; }
        public string photoKey { get; set; }
        public string url { get; set; }
        public string title { get; set; }
        public string desc { get; set; }
        public DateTime dateCreated { get; set; }
    }
}
