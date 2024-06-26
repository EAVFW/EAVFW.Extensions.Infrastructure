
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
        public static IServiceCollection ConfigureForwardedHeadersOptions(this IServiceCollection services,string knownProxiesSettings= "ForwardedHeaders:KnownProxies")
        {
            services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((options, configuration) =>
           {
               var knownProxies = configuration.GetSection(knownProxiesSettings).Get<string[]>()
                   ?.Select(IPAddress.Parse) ?? Array.Empty<IPAddress>();

               foreach (var proxie in knownProxies)
                   options.KnownProxies.Add(proxie);

               if (!knownProxies.Any())
               {
                   options.KnownProxies.Clear();
                   options.KnownNetworks.Clear();
                    
               }


               options.ForwardedHeaders = ForwardedHeaders.All;
           });

            return services;

        }
    }
}
