using LLamaWebAPI.Core;

namespace LLamaWebAPI.Services
{
    public class LLamaGrpcService
    {
        private readonly SessionManager _sessionManager;
        private readonly ILogger _logger;

        public LLamaGrpcService(SessionManager sessionManager, ILoggerFactory loggerFactory)
        {
            _sessionManager = sessionManager;
            _logger = loggerFactory.CreateLogger("Streaming");
        }

        public async Task StartSessionAndStream(string customerId, Action<string> handleMessage, CancellationToken cancellationToken)
        {
            var session = _sessionManager.GetOrCreateSession(customerId);
            await session.StartListeningAsync(handleMessage, cancellationToken);
        }

        public async Task SendMessageAsync(string customerId, string message)
        {
            var session = _sessionManager.GetOrCreateSession(customerId);
            await session.SendMessageAsync(message);
        }

        public void EndSession(string customerId)
        {
            _sessionManager.RemoveSession(customerId);
        }

        internal async Task CompleteStreamAsync(string customerId)
        {
            var session = _sessionManager.GetOrCreateSession(customerId);
            await session.CompleteStreamAsync();

            _sessionManager.RemoveSession(customerId);
        }
    }
}
