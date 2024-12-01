using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LLamaWebAPI.Core
{
    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, Session> _sessions = new ConcurrentDictionary<string, Session>();
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public SessionManager(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger("Streaming");
        }

        internal Session GetOrCreateSession(string customerId)
        {
            if (_sessions.TryGetValue(customerId, out var existingSession))
            {
                _logger.LogInformation("Reusing existing session for customerId: {CustomerId}", customerId);
                return existingSession;
            }
            else
            {
                var session = CreateNewSession(customerId);
                _sessions.TryAdd(customerId, session);
                _logger.LogInformation("Creating new session for customerId: {CustomerId}", customerId);

                return session;
            }
        }

        public void RemoveSession(string customerId)
        {
            if (_sessions.TryRemove(customerId, out _))
            {
                _logger.LogInformation("Removed session for customerId: {CustomerId}", customerId);
            }
            else
            {
                _logger.LogWarning("Failed to remove session for customerId: {CustomerId}. Session not found.", customerId);
            }
        }

        private Session CreateNewSession(string customerId)
        {

            var session = new Session(customerId, _loggerFactory);

            return session;
        }
    }
}
