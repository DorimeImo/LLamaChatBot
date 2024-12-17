using LLamaWebAPI.Configurations;
using LLamaWebAPI.Core;
using LLamaWebAPI.Core.Interfaces;
using LLamaWebAPI.Core.Middleware;
using LLamaWebAPI.Data;
using LLamaWebAPI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
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

            //Dmitry: here we need to narrow to the ui domain only (port in my case)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
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
            app.UseCors("AllowAllOrigins");

            app.UseHttpsRedirection();

            app.UseAuthorization();

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

}

