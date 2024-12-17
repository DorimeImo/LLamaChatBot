using Azure.Core;
using LLamaWebAPI.Core.Interfaces;
using LLamaWebAPI.Core.Models;
using LLamaWebAPI.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;

namespace LLamaWebAPI.Services
{
    public class AuthService: IAuthService
    {
        private readonly IAuthRepository _authRepository; 
        private readonly JwtService _jwtService;
        private readonly ILogger _logger;

        public AuthService(IAuthRepository userRepository, JwtService jwtService, ILoggerFactory loggerFactory)
        {
            _authRepository = userRepository;
            _jwtService = jwtService;
            _logger = loggerFactory.CreateLogger("Security");
        }

        public async Task<(string AccessToken, string RefreshToken, string UserId)> CreateNewUser(string username, string password, string email)
        {
            if (await _authRepository.GetUserByUsername(username) != null)
            {
                _logger.LogWarning("User already exists.");
                throw new InvalidOperationException("User already exists.");
            }

            var newUser = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Email = email,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
            };

            newUser = await _authRepository.CreateOrUpdateUser(newUser); 

            if(string.IsNullOrEmpty(newUser.Id))
            {
                _logger.LogError("Failed to persist a new user.");
                throw new InvalidOperationException($"Failed to generate a user");
            }

            try
            {
                var tokens = _jwtService.GenerateTokens(newUser.Id);
                var userRefreshToken = new Token
                {
                    UserId = newUser.Id,
                    HashedRefreshToken = HashValue(tokens.RefreshToken),
                    TokenExpiration = _jwtService.GetTokenExpirationDay()
                };

                await _authRepository.CreateOrUpdateToken(userRefreshToken);

                return ( tokens.AccessToken, tokens.RefreshToken, newUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to generate Tokens for the user with userID: {id}. \nException: {ex}.", newUser.Id, ex);
                throw new InvalidOperationException($"Failed to generate Tokens for the user.");
            }
        }

        public async Task<(string NewAccessToken, string NewRefreshToken)> RefreshTokens(string userId, string providedRefreshToken)
        {
            var user = await _authRepository.GetUserById(userId);
            var token = await _authRepository.GetTokenByUserId(userId);

            if (user == null || token == null)
            {
                _logger.LogWarning("User with id {id} or associated token not found. Authorization denied.", userId);
                throw new UnauthorizedAccessException($"User or associated token not found. Authorization denied.");
            }

            if (ValidateRefreshTokenValue(providedRefreshToken, token))
            {
                try
                {
                    var tokens = _jwtService.RefreshTokens(userId);

                    var newToken = new Token
                    {
                        UserId = userId,
                        HashedRefreshToken = HashValue(tokens.NewRefreshToken),
                        TokenExpiration = _jwtService.GetTokenExpirationDay()
                    };

                    await _authRepository.CreateOrUpdateToken(newToken);

                    return tokens;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to refresh Tokens for the user with id: {id}. \nException: {ex}", userId, ex);
                    throw new InvalidOperationException($"Failed to refresh Tokens for the user.");
                }
            }
            else 
                throw new UnauthorizedAccessException("Provided RefreshToken is not valid. Authorization denied.");
        }

        public async Task <(string AccessToken, string RefreshToken)> AuthenticateUser(string username, string password)
        {
            var user = await _authRepository.GetUserByUsername(username);

            if (user == null || !VerifyHashedValue(password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid username or password.");
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            try
            {
                var tokens = _jwtService.GenerateTokens(user.Id);

                var refToken = new Token
                {
                    UserId = user.Id,
                    HashedRefreshToken = HashValue(tokens.RefreshToken),
                    TokenExpiration = _jwtService.GetTokenExpirationDay()
                };

                await _authRepository.CreateOrUpdateToken(refToken);

                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to generate Tokens for the user with id: {id}. \nException: {ex}.", user.Id, ex);
                throw new InvalidOperationException($"Failed to generate Tokens .");
            }
        }

        public async Task RevokeRefreshToken(string userId)
        {
            var token = await _authRepository.GetTokenByUserId(userId);

            if (token == null || token.IsRevoked)
            {
                _logger.LogWarning("Attempt to revoke an invalid or already revoked token for user with userID {userId}.", userId);
                throw new InvalidOperationException("Invalid token.");
            }
            token.IsRevoked = true;

            await _authRepository.CreateOrUpdateToken(token);
        }

        public ClaimsPrincipal? ValidateAccessToken(string accessToken)
        {
            return _jwtService.ValidateAccessToken(accessToken);
        }

        private bool ValidateRefreshTokenValue(string providedValue, Token token)
        {
            if (token.TokenExpiration < DateTime.Today)
            {
                _logger.LogWarning("Refresh token expired. Expiration date: {expireDate}", token.TokenExpiration);
                return false;
            }

            if (!VerifyHashedValue(providedValue, token.HashedRefreshToken))
            {
                _logger.LogWarning("Provided refresh token does not match the stored hashed value.");
                return false;
            }

            if (token.IsRevoked)
            {
                _logger.LogWarning("Provided refresh token is revoked.");
                return false;
            }

            return true;
        }

        private bool VerifyHashedValue(string providedValue, string storedHashedValue)
        {
            return BCrypt.Net.BCrypt.Verify(providedValue, storedHashedValue);
        }

        private string HashValue(string value)
        {
            return BCrypt.Net.BCrypt.HashPassword(value);
        }
    }
}
