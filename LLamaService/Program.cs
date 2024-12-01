using System;
using System.Collections.Generic;
using LLama.Common;
using LLama;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using LLamaServer.Authentication;
using Microsoft.Extensions.DependencyInjection;
using LLamaServer.Services;
using Microsoft.Extensions.Configuration;
using LLamaServer.Tests;
using LLamaServer.Data;
using Microsoft.EntityFrameworkCore;
using LLamaServer.Core;
using LLamaServer.Messaging;
using LLamaServer.Models;


class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("Config/AppSettings.json", optional: false, reloadOnChange: true);

        builder.Services.AddMemoryCache();

        builder.Services.AddSingleton<JwtService>();

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDatabase")));

        builder.Services.AddScoped<IUserRepository, UserRepository>();

        builder.Services.AddScoped<PasswordService>();

        builder.Services.AddSingleton<LLamaService>(sp =>
    new LLamaService(@"C:\Users\dmitr\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\OllamaGGUF\Meta-Llama-3.1-8B-Instruct-Q6_K.gguf"));

        builder.Services.AddSingleton<SessionManager>();

        builder.Services.AddGrpc();

        var app = builder.Build();

        //JwtServiceTest jwtServiceTest = new JwtServiceTest();
        //jwtServiceTest.run(builder);

        app.MapGrpcService<AuthServiceImpl>();
        app.MapGrpcService<ChatServiceImpl>();
        app.MapGet("/", () => "gRPC server is running...");

        app.Run("https://localhost:5001");
    }
}



