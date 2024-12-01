using Grpc.Core;
using LLamaServer.Authentication;
using LLamaServer.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLamaServer.ChatGrpc;
using System.Security.Claims;
using System.Diagnostics;

namespace LLamaServer.Services
{
    public class ChatServiceImpl : ChatService.ChatServiceBase
    {
        private readonly JwtService _jwtService;
        private readonly SessionManager _sessionManager;

        public ChatServiceImpl(JwtService jwtService, SessionManager sessionManager)
        {
            _jwtService = jwtService;
            _sessionManager = sessionManager;
        }

        public override async Task ChatStream(
        IAsyncStreamReader<ChatMessageRequest> requestStream,
        IServerStreamWriter<ChatMessageResponse> responseStream,
        ServerCallContext context)
        {
            Console.WriteLine("DEBUG LLAMASEVER  ChatStream" );
            await foreach (var request in requestStream.ReadAllAsync())
            {
                
                //var principal = _jwtService.ValidateAccessToken(request.AccessToken);
                //if (principal == null)
                //{
                //    await responseStream.WriteAsync(new ChatMessageResponse
                //    {
                //        Message = "Invalid access token",
                //        IsFinal = true
                //    });
                //    return;
                //}

                //var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (userId == null)
                //{
                //await responseStream.WriteAsync(new ChatMessageResponse
                //{
                //    Message = "User information not found in token",
                //    IsFinal = true
                //});
                //return;
                //}

                string userId = "1";

                var session = _sessionManager.GetOrCreateSession(userId);

                await foreach (var responseText in session.ProcessMessageAsync(request.Message))
                {
                    await responseStream.WriteAsync(new ChatMessageResponse
                    {
                        Message = responseText,
                        IsFinal = false
                    });
                }

                await responseStream.WriteAsync(new ChatMessageResponse
                {
                    IsFinal = true
                });
            }
        }
    }
}
