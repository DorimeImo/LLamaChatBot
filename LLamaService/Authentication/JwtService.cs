using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LLamaServer.Authentication
{
    public class JwtService
    {

        private readonly X509Certificate2 _certificate; // Certificate for signing tokens
        private readonly IMemoryCache _cache;
        private readonly int _accessTokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        private readonly string _audience = "LLama-Server";
        private readonly string _issuer = "https://llama-server.com";  // Define your issuer

        public JwtService(IConfiguration configuration, IMemoryCache cache) 
        {
            _cache = cache;

            string print = configuration["JWTSettings:CertificateThumbprint"];

            _certificate = GetCertificate(print);

            _accessTokenExpirationMinutes = int.Parse(configuration["JWTSettings:AccessTokenExpirationMinutes"]);
            _refreshTokenExpirationDays = int.Parse(configuration["JWTSettings:RefreshTokenExpirationDays"]);
        }

        private X509Certificate2 GetCertificate(string thumbprint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);

                return certCollection.Count > 0 ? certCollection[0] : throw new InvalidOperationException("Certificate not found");
            }

        }

        public (string AccessToken, string RefreshToken) GenerateTokens(string userId)
        {
            var accessToken = GenerateAccessToken(userId);
            var refreshToken = GenerateRefreshToken();

            // Store the hashed refresh token in cache (or database)
            var hashedRefreshToken = HashToken(refreshToken);
            _cache.Set(userId, hashedRefreshToken, TimeSpan.FromDays(_refreshTokenExpirationDays));

            return (accessToken, refreshToken);
        }

        private string GenerateAccessToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                Audience = _audience,  // Set the audience here
                Issuer = _issuer,      // Set the issuer here
                SigningCredentials = new X509SigningCredentials(_certificate, SecurityAlgorithms.RsaSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        public ClaimsPrincipal ValidateAccessToken(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new X509SecurityKey(_certificate),

                // Audience validation
                ValidateAudience = true,
                ValidAudience = _audience,  // Set to match the Audience value in GenerateAccessToken

                // Issuer validation
                ValidateIssuer = true,
                ValidIssuer = _issuer,  // Set to match the Issuer value in GenerateAccessToken

                // Lifetime and other validations
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

                // If token type or signature algorithm is invalid
                return null;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Token validation failed: {ex.Message}");
                return null;
            }
        }

        public (string NewAccessToken, string NewRefreshToken) RefreshTokens(string userId, string providedRefreshToken)
        {
            // Retrieve the stored hashed refresh token
            if (!_cache.TryGetValue(userId, out string storedHashedToken))
                throw new SecurityTokenException("Invalid refresh token");

            // Compare hashes
            if (storedHashedToken != HashToken(providedRefreshToken))
                throw new SecurityTokenException("Invalid refresh token");

            // Generate new tokens
            var newAccessToken = GenerateAccessToken(userId);
            var newRefreshToken = GenerateRefreshToken();

            // Store the new hashed refresh token
            var newHashedRefreshToken = HashToken(newRefreshToken);
            _cache.Set(userId, newHashedRefreshToken, TimeSpan.FromDays(_refreshTokenExpirationDays));

            return (newAccessToken, newRefreshToken);
        }

        // Hash the Refresh Token for secure storage
        private string HashToken(string token)
        {
            using (var sha256 = SHA256.Create())
            {
                var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
                var hashBytes = sha256.ComputeHash(tokenBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
