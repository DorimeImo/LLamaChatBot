using Azure.Core;
using LLamaWebAPI.Core.Interfaces;
using LLamaWebAPI.Core.Models.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
                return Ok(tokens);
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

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshTokens([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
                {
                    return Unauthorized(new { Message = "Refresh token not found." });
                }

                var tokens = await _authService.RefreshTokens(request.UserId, refreshToken);

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
                _logger.LogWarning("RefreshToken(...) failed for user with id {id}", request.UserId);
                return Unauthorized(new { Message = "Invalid or expired refresh token." , Details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error occurred in RefreshToken(...)");
                return StatusCode(500, new { Message = "An unexpected error occurred.", Details = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            try
            {
                await _authService.RevokeRefreshToken(request.UserId);
                return Ok(new { Message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in Logout(...) for user with id: {if}", request.UserId);
                return StatusCode(500, new { Message = "Failed to logout. An unexpected error occurred." });
            }
        }

        [HttpPost("verifyToken")]
        public async Task <IActionResult> VerifyToken(string accessToken)
        {
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
