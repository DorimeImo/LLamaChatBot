using LLamaWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LLamaWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamingController : ControllerBase
    {
        private readonly LLamaGrpcService _llamaGrpcService;
        private readonly AuthService _authService;
        private readonly ILogger _logger;

        public StreamingController(LLamaGrpcService llamaGrpcService, AuthService authService, ILoggerFactory loggerFactory)
        {
            _llamaGrpcService = llamaGrpcService;
            _authService = authService;
            _logger = loggerFactory.CreateLogger("Streaming");
        }

        [HttpGet("stream")]
        public async Task StartStreamData(string accessToken, string message, CancellationToken cancellationToken)
        {
            Response.ContentType = "text/event-stream";

            var principal = _authService.ValidateAccessToken(accessToken);

            if (principal != null)
            {
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Bad Request: Missing or invalid user ID in the access token.");

                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    await Response.WriteAsync("Bad Request: Missing or invalid user ID in the access token.");
                    return;
                }

                _logger.LogInformation("Received request from userId: {userId} ; message : {message}", userId, message);

                var sessionId = userId;

                // Start listening for responses asynchronously
                try
                {
                    await _llamaGrpcService.StartSessionAndStream(sessionId, async responseMessage =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                await Response.WriteAsync($"data: {responseMessage.Replace("\n", "\\n")}\n\n");
                                await Response.Body.FlushAsync();
                            }
                            catch (Exception e)
                            {
                                _logger.LogError("Failed to write the responseMessage({responseMessage}) to Response stream. \n Error: {e}", responseMessage, e);
                            }
                        }
                    }, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Request was cancelled by the client.");
                    Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred during the request.");
                    Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await Response.WriteAsync("Internal Server Error");
                }

                try
                {
                    await _llamaGrpcService.SendMessageAsync(sessionId, message);
                }
                catch(Exception e)
                {
                    _logger.LogError("Error processing request for session with sessionId: {sessionID}. \n {Error}: ", sessionId, e);
                    Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await Response.WriteAsync("Internal Server Error");
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }

                _logger.LogInformation("Cancellation token was presented.");

                await _llamaGrpcService.CompleteStreamAsync(sessionId);

                _logger.LogInformation("Stream was completed successfully");
            }
            else
            {
                _logger.LogWarning("Unauthorized access attempt: invalid or expired token.");

                Response.StatusCode = StatusCodes.Status401Unauthorized;
                await Response.WriteAsync("Unauthorized: Invalid or expired token.");

                return;
            }
        }
    }
}

