using System.ComponentModel.DataAnnotations;

namespace LLamaWebAPI.Core.Models.Requests
{
    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; }
        [Required]
        [MinLength(6)]
        public string Password { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
