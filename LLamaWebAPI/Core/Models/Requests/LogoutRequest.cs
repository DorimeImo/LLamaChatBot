using System.ComponentModel.DataAnnotations;

namespace LLamaWebAPI.Core.Models.Requests
{
    public class LogoutRequest
    {
        [Required]
        public string UserId { get; set; }
    }
}
