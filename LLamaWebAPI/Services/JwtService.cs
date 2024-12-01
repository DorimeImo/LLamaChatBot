using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using LLamaWebAPI.Configurations;

namespace LLamaWebAPI.Services
{
    public class JwtService
    {
        // Certificate to sign tokens
        private readonly X509Certificate2 _certificate;
        private readonly JwtConfigs _configs;

        private readonly ILogger _logger;

        public JwtService(IConfiguration configuration, ILoggerFactory loggerFactory, IOptions<JwtConfigs> jwtConfigs)
        {
            _logger = loggerFactory.CreateLogger("Security");
            _configs = jwtConfigs.Value;

            ValidateJwtConfigs();

            _certificate = GetCertificate(_configs.CertificateThumbprint);
        }

        public DateTime GetTokenExpirationDay()
        {
            return DateTime.UtcNow.AddDays(_configs.RefreshTokenExpirationDays);
        }

        private X509Certificate2 GetCertificate(string thumbprint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);

                if (certCollection.Count > 0)
                    return certCollection[0];
                else
                {
                    _logger.LogError("Critical configuration error: Unable to load encryption certificate.");
                    throw new InvalidOperationException("Certificate not found");
                }
            }
        }

        public (string AccessToken, string RefreshToken) GenerateTokens(string userId)
        {
            _logger.LogInformation("Generating tokens for user with id {id}", userId);
            var accessToken = GenerateAccessToken(userId);
            var refreshToken = GenerateRefreshToken(userId);

            return (accessToken, refreshToken);
        }

        public (string NewAccessToken, string NewRefreshToken) RefreshTokens(string userId)
        {
            _logger.LogInformation("Refreshing tokens for user with id {id}", userId);
            var newAccessToken = GenerateAccessToken(userId);
            var newRefreshToken = GenerateRefreshToken(userId);

            return (newAccessToken, newRefreshToken);
        }

        private string GenerateAccessToken(string userId)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }),
                    Expires = DateTime.UtcNow.AddMinutes(_configs.AccessTokenExpirationMinutes),
                    Audience = _configs.Audience,
                    Issuer = _configs.Issuer,
                    SigningCredentials = new X509SigningCredentials(_certificate, SecurityAlgorithms.RsaSha256)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to generate AccessToken for user with id {id}. " +
                    "\nException: {ex}", userId, ex);
                throw;
            }
        }

        private string GenerateRefreshToken(string userId)
        {
            try
            {
                var randomNumber = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomNumber);
                }
                return Convert.ToBase64String(randomNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to generate RefreshToken for user with id {id}. " +
                    "\nException: {ex}", userId, ex);
                throw;
            }
        }

        public ClaimsPrincipal? ValidateAccessToken(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new X509SecurityKey(_certificate),
                ValidateAudience = true,
                ValidAudience = _configs.Audience,
                ValidateIssuer = true,
                ValidIssuer = _configs.Issuer,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(accessToken, validationParameters, out SecurityToken validatedToken);

                // Additional validation checks (e.g., token type, claims, etc.)
                if (validatedToken is JwtSecurityToken jwtToken &&
                    jwtToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return principal;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("AccessToken validation failed. Exception: {ex}", ex);
                return null;
            }
        }

        public void ValidateJwtConfigs()
        {
            if (_configs != null)
            {
                if (string.IsNullOrEmpty(_configs.CertificateThumbprint) ||
                        string.IsNullOrEmpty(_configs.Audience) ||
                        string.IsNullOrEmpty(_configs.Issuer) ||
                        _configs.AccessTokenExpirationMinutes <= 0 ||
                        _configs.RefreshTokenExpirationDays <= 0)
                {
                    _logger.LogError("Encryption certificate cant be loaded. " +
                    "\nThumbprint: {print}; " +
                    "\nAudience: {audience} " +
                    "\nIssuer: {issuer} " +
                    "\nAccessTokenExpirationMinutes: {_accessTokenExpirationMinutes}; " +
                    "\nRefreshTokenExpirationDays: {_refreshTokenExpirationDays}.",
                    string.IsNullOrEmpty(_configs.CertificateThumbprint) ? "" : "**presented**",
                    _configs.Audience, _configs.Issuer,
                    _configs.AccessTokenExpirationMinutes, _configs.RefreshTokenExpirationDays);

                    throw new InvalidOperationException("Critical configuration failure: JWTSettings contains missing or invalid values.");
                }
            }
        }
    }
}
