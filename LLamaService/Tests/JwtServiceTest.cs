using LLamaServer.Authentication;
using LLamaServer.Data;
using LLamaServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLamaServer.Tests
{
    public class JwtServiceTest
    {
        public void run(WebApplicationBuilder builder)
        {
            // Build the ServiceProvider manually
            var serviceProvider = builder.Services.BuildServiceProvider();

            // Retrieve the JwtService manually
            var jwtService = serviceProvider.GetRequiredService<JwtService>();
            var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
            var passService = serviceProvider.GetRequiredService<PasswordService>();

            AuthServiceImpl authServiceImpl = new AuthServiceImpl(jwtService, userRepository, passService);
        }
    }
}
