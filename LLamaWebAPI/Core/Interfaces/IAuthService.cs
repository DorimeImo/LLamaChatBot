namespace LLamaWebAPI.Core.Interfaces
{
    public interface IAuthService
    {
        Task<(string AccessToken, string RefreshToken)> CreateNewUser(string username, string password, string email);
        Task<(string NewAccessToken, string NewRefreshToken)> RefreshTokens(string userId, string username, string providedRefreshToken);
        Task<(string AccessToken, string RefreshToken)> AuthenticateUser(string username, string password);
        Task RevokeRefreshToken(string userId, string username);
    }
}
