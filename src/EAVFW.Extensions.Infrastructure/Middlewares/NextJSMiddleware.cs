using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EAVFW.Extensions.Infrastructure
{
    public class NextJSMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<NextJSMiddleware> _logger;
        private readonly Dictionary<string, Regex> _routes;

        public NextJSMiddleware(RequestDelegate next, IWebHostEnvironment environment, ILogger<NextJSMiddleware> logger)
        {
            _next = next;
            _environment = environment;
            _logger = logger;

            if (environment.IsLocal())
            {
                _routes = File.Exists($"{environment.ContentRootPath}/.next/routes-manifest.json") ?
                    JToken.Parse(File.ReadAllText($"{environment.ContentRootPath}/.next/routes-manifest.json"))
                    .SelectToken("$.dynamicRoutes").ToDictionary(k => k.SelectToken("$.page") + "/index.html", v => new Regex(v.SelectToken("$.regex").ToString()))
                    : new Dictionary<string, Regex>();
            }
            else
            {
                _routes = File.Exists($"{environment.ContentRootPath}/routes-manifest.json") ?
                   JToken.Parse(File.ReadAllText($"{environment.ContentRootPath}/routes-manifest.json"))
                   .SelectToken("$.dynamicRoutes").ToDictionary(k => k.SelectToken("$.page") + "/index.html", v => new Regex(v.SelectToken("$.regex").ToString()))
                   : new Dictionary<string, Regex>();
            }

            logger.LogInformation("Intialized Routes : " + string.Join(",", _routes.Keys));

        }

        public async Task Invoke(HttpContext httpContext)
        {

            var matched = _routes.FirstOrDefault(k => k.Value.IsMatch(httpContext.Request.Path));
            if (!matched.Equals(default(KeyValuePair<string, Regex>)))
            {
                _logger.LogInformation("Route matched with NextJS Route: " + matched.Key);
                httpContext.Request.Path = $"{matched.Key}";
            }
            await _next(httpContext);
        }

    }
}
