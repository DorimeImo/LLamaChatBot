using Grpc.Core;
using LLamaServer.Authentication;
using LLamaServer.Data;
using LLamaServer.AuthGrpc;
using Microsoft.AspNetCore.Identity.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LoginRequest = LLamaServer.AuthGrpc.LoginRequest;

namespace LLamaServer.Services
{
    public class AuthServiceImpl : AuthService.AuthServiceBase
    {
        private readonly JwtService _jwtService;
        private readonly IUserRepository _userRepository;
        private readonly PasswordService _passwordService;

        public AuthServiceImpl(JwtService jwtService, IUserRepository userRepository, PasswordService passwordService)
        {
            this._jwtService = jwtService;
            this._userRepository = userRepository;
            this._passwordService = passwordService;
        }

        public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
        {
            var user = await _userRepository.GetUserByUsernameAsync(request.Username);

            // If the user doesn't exist or the password is incorrect, return an error
            if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Generate access and refresh tokens for the authenticated user
            var tokens = _jwtService.GenerateTokens(user.UserId.ToString());

            return new LoginResponse
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                Success = true,
                Message = "Login successful"
            };
        }
    }
}
