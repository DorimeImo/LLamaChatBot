using LLama;
using LLama.Common;
using LLamaServer.Core;
using LLamaServer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLamaServer.Messaging
{
    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, Session> _sessions = new ConcurrentDictionary<string, Session>();
        private readonly LLamaService _llamaService;

        public SessionManager(LLamaService llamaService)
        {
            _llamaService = llamaService;
        }

        public Session GetOrCreateSession(string userId)
        {
            return _sessions.GetOrAdd(userId, _ => CreateNewSession());
        }

        public void RemoveSession(string userId)
        {
            _sessions.TryRemove(userId, out _);
        }

        private Session CreateNewSession()
        {
            var executor = _llamaService.CreateExecutor();
            var model = _llamaService.Model;
            return new Session(executor, model);
        }
    }
}
