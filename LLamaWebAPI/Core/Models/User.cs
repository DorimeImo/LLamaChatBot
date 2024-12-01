namespace LLamaWebAPI.Core.Models
{
    public class User
    {
        public string Id { get; set; } // Unique identifier, e.g., GUID
        public string Username { get; set; }
        public string PasswordHash { get; set; } // Hashed password for security
        public string Email { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
