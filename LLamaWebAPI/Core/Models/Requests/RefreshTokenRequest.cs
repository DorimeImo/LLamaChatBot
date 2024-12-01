using System.ComponentModel.DataAnnotations;

namespace LLamaWebAPI.Core.Models.Requests
{
    public class RefreshTokenRequest
    {
        public string UserId { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string RefreshToken { get; set; }
    }
}
