namespace StorageWebAppBackend.Models
{
    public class PhotoMetadata
    {
            public string id { get; set; }
            public string userId { get; set; }
            public string photoKey { get; set; }
            public string fileName { get; set; }
            public string title { get; set; }
            public string desc { get; set; }
            public DateTime dateCreated { get; set; }
    }
}
