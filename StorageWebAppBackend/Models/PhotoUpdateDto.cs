namespace StorageWebAppBackend.Models
{
    public class PhotoUpdateDto
    {
        public string UserId { get; set; }
        public string? Title { get; set; }
        public string? Desc { get; set; }
    }
}
