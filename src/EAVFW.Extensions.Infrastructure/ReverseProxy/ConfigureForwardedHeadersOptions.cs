
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConfigureForwardedHeadersOptionsExtensions
    {
        public static IServiceCollection ConfigureForwardedHeadersOptions(this IServiceCollection services,string setting= "ForwardedHeaders:KnownProxies")
        {
            services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((options, configuration) =>
           {
               foreach (var proxie in configuration.GetSection(setting).Get<string[]>()
                   ?.Select(IPAddress.Parse) ?? Array.Empty<IPAddress>())
                   options.KnownProxies.Add(proxie);

               options.ForwardedHeaders =
                   ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
           });

            return services;

        }
    }
}
