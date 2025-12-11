namespace StorageWebAppBackend.Models
{
    public class EmailSettings
    {
        public string SMTPHost { get; set; }
        public int SMTPPort { get; set; }
        public string SenderEmail { get; set; }
        public string SenderPassword { get; set; }
    }
}
