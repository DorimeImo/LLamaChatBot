using LLamaWebAPI.Core.Models;

namespace LLamaWebAPI.Data
{
    public interface IAuthRepository
    {
        //USERS
        Task <User?> GetUserById(string id);
        Task<User> CreateOrUpdateUser(User user);
        Task<User?> GetUserByUsername(string username);

        //TOKENS
        Task<Token?> GetTokenByUserId(string userId);
        Task<Token> CreateOrUpdateToken(Token refreshToken);
    }
}
