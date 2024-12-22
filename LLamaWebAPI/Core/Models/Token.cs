using System.ComponentModel.DataAnnotations;

namespace LLamaWebAPI.Core.Models
{
    public class Token
    {
        public int TokenId { get; set; } 
        public int UserId { get; set; } 
        public string HashedRefreshToken { get; set; }
        public DateTime TokenExpiration { get; set; }
        public bool IsRevoked { get; set; }
    }
}
