using NLog;

namespace LLamaWebAPI.Core.Middleware
{
    public class TraceIdentifierLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public TraceIdentifierLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context?.TraceIdentifier != null)
            {
                using (ScopeContext.PushProperty("TraceId", context.TraceIdentifier))
                {
                    await _next(context);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
