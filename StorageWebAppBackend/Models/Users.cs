namespace StorageWebAppBackend.Models
{
    public class Users
    {
        public string id { get; set; }               // required by Cosmos
        public string email { get; set; }            // matches /email partition key
        public string name { get; set; }             // optional
        public string passwordHash { get; set; }     // consistent casing
        public string dateCreated { get; set; }      // ISO 8601 string
        public string? Banner { get; set; }
        public string? ProfileImage { get; set; }
    }
}
