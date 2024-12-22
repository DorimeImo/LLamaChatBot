using System.ComponentModel.DataAnnotations;

namespace LLamaWebAPI.Core.Models
{
    public class User
    {
        public int Id { get; set; } 
        public string Username { get; set; }
        public string PasswordHash { get; set; } 
        public string Email { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
