using System.Security.Claims;

namespace LLamaWebAPI.Core.Interfaces
{
    public interface IAuthService
    {
        Task<(string AccessToken, string RefreshToken, int UserId)> CreateNewUser(string username, string password, string email);
        Task<(string NewAccessToken, string NewRefreshToken)> RefreshTokens(int userId, string providedRefreshToken);
        Task<(string AccessToken, string RefreshToken, int UserId)> AuthenticateUser(string username, string password);
        Task RevokeRefreshToken(int userId);
        ClaimsPrincipal? ValidateAccessToken(string accessToken);
    }
}
