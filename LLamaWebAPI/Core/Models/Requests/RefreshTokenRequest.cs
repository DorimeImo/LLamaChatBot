using System.ComponentModel.DataAnnotations;

namespace LLamaWebAPI.Core.Models.Requests
{
    public class RefreshTokenRequest
    {
        [Required]
        public string UserId { get; set; }
    }
}
