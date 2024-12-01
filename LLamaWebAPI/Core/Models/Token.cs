namespace LLamaWebAPI.Core.Models
{
    public class Token
    {
        public string TokenId { get; set; } // Primary key
        public string UserId { get; set; } // Foreign key to User
        public string HashedRefreshToken { get; set; }
        public DateTime TokenExpiration { get; set; }
        public bool IsRevoked { get; set; }
    }
}
