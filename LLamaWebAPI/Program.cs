using LLamaWebAPI.Configurations;
using LLamaWebAPI.Core;
using LLamaWebAPI.Core.Interfaces;
using LLamaWebAPI.Core.Middleware;
using LLamaWebAPI.Data;
using LLamaWebAPI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

public class Program
{

    public static void Main(string[] args)
    {
        var logger = LogManager.GetLogger("General");
        
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Host.UseNLog();

        logger.Debug("Application is starting up...");

        try
        {
            builder.Services.AddControllers();
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ValidateModelAttribute>();
            });

            builder.Services.AddSingleton<SessionManager>();
            builder.Services.AddScoped<LLamaGrpcService>();

            //Dmitry: check if it is needed. Delete otherwise
            builder.Services.AddMemoryCache();

            builder.Services.Configure<JwtConfigs>(builder.Configuration.GetSection("JWTSettings"));
            builder.Services.AddSingleton<JwtService>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();


            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDatabase")));

            //Dmitry: here we need to narrow down to the ui domain only (port in my case)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:3000") 
                          .AllowAnyHeader()                     
                          .AllowAnyMethod()                     
                          .AllowCredentials();                  
                });
            });

            //middleware against Brute-force and DoS attacks
            builder.Services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("default", limiter =>
                {
                    limiter.PermitLimit = 100;
                    limiter.Window = TimeSpan.FromMinutes(1);
                });
            });

            var app = builder.Build();

            app.UseMiddleware<TraceIdentifierLoggingMiddleware>();

            //Dmitry: this needs to be modified to AllowFrontend
            app.UseCors("AllowFrontend");

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            };
            app.UseWebSockets(webSocketOptions);

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.Map("/api/wschat", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await HandleWebSocketAsync(webSocket, context);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            });

            app.MapControllers();

            app.Run();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Application failed to start");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }


    private static async Task HandleWebSocketAsync(WebSocket webSocket, HttpContext context)
    {
        var logger = LogManager.GetLogger("Streaming");

        logger.Debug("WebSocket connection established.");

        bool isAuthenticated = false;
        string userId = null;

        var llamaService = context.RequestServices.GetRequiredService<LLamaGrpcService>();

        //Dmitry: how many words is it?
        var buffer = new byte[4 * 1024];
        WebSocketReceiveResult result = null;

        try
        {
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                var authPayload = JsonSerializer.Deserialize<AuthPayload>(receivedMessage);

                if (authPayload != null && authPayload.Type == "authenticate")
                {
                    var jwtService = context.RequestServices.GetRequiredService<JwtService>();
                    var principal = jwtService.ValidateAccessToken(authPayload.Token);
                    if (principal != null)
                    {
                        userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            isAuthenticated = true;
                            logger.Info("WebSocket connection authenticated for userId: {UserId}", userId);
                        }
                    }
                }

                if (!isAuthenticated)
                {
                    logger.Warn("WebSocket connection rejected: Invalid authentication.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid authentication", CancellationToken.None);
                    return;
                }
            }
            else
            {
                logger.Warn("WebSocket connection rejected: First message not text.");
                await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "First message must be authentication", CancellationToken.None);
                return;
            }

            var cancellationToken = context.RequestAborted;

            var streamingTask = llamaService.StartSessionAndStream(userId, async responseMessage =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var sanitizedMessage = responseMessage.Replace("\n", "\\n");
                        var messageBytes = Encoding.UTF8.GetBytes(sanitizedMessage);
                        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        logger.Error("Failed to send message to WebSocket. Error: {Error}", e);
                    }
                }
            }, cancellationToken);

            while (!webSocket.CloseStatus.HasValue && !cancellationToken.IsCancellationRequested)
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.Info("WebSocket connection closed by client.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    logger.Info("Received message from userId {UserId}: {Message}", userId, receivedMessage);

                    await llamaService.SendMessageAsync(userId, receivedMessage);
                }
            }

            await streamingTask;
        }
        catch (OperationCanceledException)
        {
            logger.Info("WebSocket connection cancelled.");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "WebSocket connection error.");
        }
        finally
        {
            if (webSocket.State != WebSocketState.Closed)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            webSocket.Dispose();
            logger.Debug("WebSocket connection closed.");
        }
    }

    public class AuthPayload
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}

