using Azure.Core;
using LLamaWebAPI.Core.Interfaces;
using LLamaWebAPI.Core.Models.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LLamaWebAPI.Controllers
{
    //Dmitry: replace exceptions with custom more specific exceptions to incapsulate 
    //exception handling logic and keep it descriptive.
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger _logger;

        public AuthController(IAuthService authService, ILoggerFactory loggerFactory)
        {
            _authService = authService;
            _logger = loggerFactory.CreateLogger("Security");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var item in errors)
                {
                    _logger.LogWarning("Invalid input : " + item.ToString());
                }
                return BadRequest(new { Message = "Invalid input", Errors = errors });
            }

            try
            {
                var tokens = await _authService.CreateNewUser(request.Username, request.Password, request.Email);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict, // Protects against CSRF
                    Expires = DateTime.UtcNow.AddDays(14)
                };
                Response.Cookies.Append("refreshToken", tokens.RefreshToken, cookieOptions);
                Response.Cookies.Append("userId", tokens.UserId.ToString(), cookieOptions);

                return Ok(new 
                { 
                    accessToken = tokens.AccessToken,
                    userId = tokens.UserId
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "User creation failed.");
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in Register(...)");
                return StatusCode(500, new { Message = "An unexpected error occurred.", Details = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                //delay against mitigating attacks
                await Task.Delay(TimeSpan.FromMilliseconds(200));

                var tokens = await _authService.AuthenticateUser(request.Username, request.Password);
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict, // Protects against CSRF
                    Expires = DateTime.UtcNow.AddDays(14)
                };
                Response.Cookies.Append("refreshToken", tokens.RefreshToken, cookieOptions);
                Response.Cookies.Append("userId", tokens.UserId.ToString(), cookieOptions);

                return Ok(new
                {
                    accessToken = tokens.AccessToken,
                    userId = tokens.UserId
                });
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("User authorisation failed.");
                return Unauthorized(new { Message = "Invalid username or password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in Login(...)");
                return StatusCode(500, new { Message = "An unexpected error occurred.", Details = ex.Message });
            }
        }

        [HttpGet("refresh")]
        public async Task<IActionResult> RefreshTokens()
        {
            Request.Cookies.TryGetValue("refreshToken", out var refreshToken);
            Request.Cookies.TryGetValue("userId", out var userId);

            try
            {
                if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "Refresh token or userId are not found." });
                }
                _logger.LogInformation("RefreshToken(...) processing...");

                var tokens = await _authService.RefreshTokens(int.Parse(userId), refreshToken);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(14)
                };
                Response.Cookies.Append("refreshToken", tokens.NewRefreshToken, cookieOptions);

                return Ok(new
                {
                    accessToken = tokens.NewAccessToken
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("RefreshToken(...) failed for user with id {id}", userId);
                return Unauthorized(new { Message = "Invalid or expired refresh token." , Details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error occurred in RefreshToken(...)");
                return StatusCode(500, new { Message = "An unexpected error occurred.", Details = ex.Message });
            }
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Logout processing ...");

            if (Request.Cookies.TryGetValue("userId", out var userId) && !string.IsNullOrEmpty(userId))
            {
                try
                {
                    await _authService.RevokeRefreshToken(int.Parse(userId));

                    _logger.LogInformation("Logout deleting cookies.");

                    Response.Cookies.Delete("userId", new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    });

                    Response.Cookies.Delete("refreshToken", new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    });

                    _logger.LogInformation("User with id {userId} successfully logged out.", userId);
                    return Ok(new { Message = "Logged out successfully." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error occurred in Logout(...) for user with id: {userId}", userId);
                    return StatusCode(500, new { Message = "Failed to logout. An unexpected error occurred." });
                }
            }

            _logger.LogWarning("Logout request failed. User id is not present in cookies.");
            return BadRequest(new { Message = "Failed to logout. User ID is missing." });
        }

        [HttpGet("verifyToken")]
        public async Task <IActionResult> VerifyToken()
        {
            _logger.LogInformation("Access token verication.");

            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("Authorization header is missing or invalid.");
                return Unauthorized(new { Message = "Authorization header is missing or invalid." });
            }

            var accessToken = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var principal = _authService.ValidateAccessToken(accessToken);
                if (principal != null)
                    return Ok();
                else
                {
                    _logger.LogWarning("Access token verication failed.");
                    return Unauthorized(new { Message = "ccess token verication failed."});
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in Register(...)");
                return StatusCode(500, new { Message = "An unexpected error occurred.", Details = ex.Message });
            }
        }
    }
}
