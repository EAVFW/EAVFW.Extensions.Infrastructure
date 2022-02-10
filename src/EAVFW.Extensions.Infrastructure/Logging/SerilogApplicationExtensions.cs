using EAVFW.Extensions.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Builder;
using Serilog;
using System.Security.Claims;

namespace EAVFW.Extensions.Infrastructure.Logging
{
    public static class SerilogApplicationExtensions
    {
        public static IApplicationBuilder UseRequestLoggingMiddleware(this IApplicationBuilder app)
        {
            return app
                .UseMiddleware<OpenTelemetryMiddleware>()
                .UseSerilogRequestLogging(options =>
                {

                    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        var sub = httpContext?.User?.FindFirstValue("sub");
                        if (sub != null)
                        {
                            diagnosticContext.Set("SubjectId", sub);
                        }
                    };
                });
        }
    }
}
