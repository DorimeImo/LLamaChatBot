using Grpc.Core;
using Grpc.Net.Client;
using LLamaClient.ChatGrpc;
using System.Diagnostics;

namespace LLamaWebAPI.Core
{
    internal class Session
    {
        private readonly string _customerId;
        private readonly GrpcChannel _channel;
        private readonly ChatService.ChatServiceClient _client;
        private readonly AsyncDuplexStreamingCall<ChatMessageRequest, ChatMessageResponse> _chatStream;
        private readonly ILogger _logger;
        public Session(string customerId, ILoggerFactory loggerFactory)
        {
            _customerId = customerId;
            _channel = GrpcChannel.ForAddress("https://localhost:5001");
            _client = new ChatService.ChatServiceClient(_channel);
            _chatStream = _client.ChatStream();
            _logger = loggerFactory.CreateLogger("Streaming");
        }

        public async Task StartListeningAsync(Action<string> handleMessage, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var response in _chatStream.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancellation requested for session {_customerId}.", _customerId);
                        break;
                    }

                    handleMessage(response.Message);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Listening operation was cancelled for session {_customerId}.", _customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while reading the response stream.");
                throw;
            }
            
        }

        public async Task SendMessageAsync(string message)
        {
            try
            {
                await _chatStream.RequestStream.WriteAsync(new ChatMessageRequest
                {
                    Username = _customerId,
                    Message = message
                });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred while writing to request stream.");
                throw;
            }
            
        }

        public async Task CompleteStreamAsync()
        {
            try
            {
                await _chatStream.RequestStream.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to complete the request stream for session {_customerId}.", _customerId);
            }
            finally
            {
                await _channel.ShutdownAsync();
            }
        }
    }
}
