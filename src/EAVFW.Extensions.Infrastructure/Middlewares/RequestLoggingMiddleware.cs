using EAVFramework.Extensions;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EAVFW.Extensions.Infrastructure.Middlewares
{
    /// <summary>
    /// Middleware for decorating HTTP Responses with the headers 'x-correlation-id' and 'x-session-id'.
    /// Also enriches the DiagnosticContext with these properties, so that logs occuring in the event of an HTTP request
    /// are also decorated with corresponding CorrelationId and SessionId properties.
    /// </summary>
    /// <remarks>
    /// The sessionId is derived from a hash of the authentication cookie associated with the request.
    /// </remarks>
    public class OpenTelemetryMiddleware
    {
        public const string CorrelationIdHeader = "x-correlation-id";
        public const string SessionIdHeader = "x-session-id";
        public const string SessionCookieName = "eavfw";

        private readonly RequestDelegate _next;
        private readonly IDiagnosticContext _diag;

        public OpenTelemetryMiddleware(RequestDelegate next, IDiagnosticContext diagnosticContext)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _diag = diagnosticContext ?? throw new ArgumentNullException(nameof(diagnosticContext));
        }

        public Task Invoke(HttpContext context)
        {
            context.TraceIdentifier = GetCorrelationId(context);
            var sessionId = GetSessionId(context);

            _diag.Set("CorrelationId", context.TraceIdentifier);
            _diag.Set("SessionId", sessionId);

            if (!context.Request.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Request.Headers[CorrelationIdHeader] = new[] { context.TraceIdentifier };
            }

            if (!context.Request.Headers.ContainsKey(SessionIdHeader))
            {
                context.Request.Headers[SessionIdHeader] = new[] { sessionId };
            }

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
                {
                    context.Response.Headers[CorrelationIdHeader] = new[] { context.TraceIdentifier };
                }

                if (!context.Response.Headers.ContainsKey(SessionIdHeader))
                {
                    context.Response.Headers[SessionIdHeader] = new[] { sessionId };
                }

                return Task.CompletedTask;
            });

            return _next(context);
        }

        /// <summary>
        /// Get the CorrelationId from headers if it exists, or generate a new CorrelationId
        /// </summary>
        private string GetCorrelationId(HttpContext context)
        {
            var header = string.Empty;

            if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var reqValues))
            {
                header = reqValues.FirstOrDefault();
            }
            else if (context.Response.Headers.TryGetValue(CorrelationIdHeader, out var respValues))
            {
                header = respValues.FirstOrDefault();
            }

            return !string.IsNullOrEmpty(header) ? header : Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Get SessionId from headers if it exists, or generate it from a hash of the current auth cookie.
        /// </summary>
        private string GetSessionId(HttpContext context)
        {
            var header = string.Empty;

            if (context.Request.Headers.TryGetValue(SessionIdHeader, out var reqValues))
            {
                header = reqValues.FirstOrDefault();
            }
            else if (context.Response.Headers.TryGetValue(SessionIdHeader, out var respValues))
            {
                header = respValues.FirstOrDefault();
            }

            var session = context?.Request?.Cookies?.FirstOrDefault(x => x.Key == SessionCookieName);
            var sessionId = session.HasValue ? session.Value.Value.Sha256() : header;

            return sessionId;
        }


    }
}
