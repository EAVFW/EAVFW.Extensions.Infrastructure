using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EAVFW.Extensions.Infrastructure
{

    public class NextJSDocumentLoadPropagator : TextMapPropagator
    {
        public override ISet<string> Fields => new HashSet<string>();


        public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter)
        {

        }

        public override PropagationContext Extract<T>(PropagationContext context, T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            if (context.ActivityContext.IsValid())
            {
                // If a valid context has already been extracted, perform a noop.
                return context;
            }

            if (carrier is HttpRequest request)
            {
                if (request.Path.StartsWithSegments("/_next", out var id))
                {
                    request.Cookies.TryGetValue("traceparent", out var value);
                    if (ActivityContext.TryParse(value, null, out var tid))
                    {
                        return new PropagationContext(tid, context.Baggage);
                    }
                }



            }



            return context;

        }
    }

    public class CustomStream : Stream
    {
        private readonly Stream _stream;
        private readonly Encoding _encoding;
        private readonly string _pattern;
        private readonly string _replacement;
        private readonly Queue<char> _lastChars;
        // private const string ReplaceTarget = "__REPLACE_TRACEID__";
        // private const string Replacement = "HELLO WORLD";

        public CustomStream(Stream stream, Encoding encoding, string pattern, string replacement)
        {
            _stream = stream;
            _encoding = encoding;
            _pattern = pattern;
            _replacement = replacement;
            _lastChars = new Queue<char>(pattern.Length);
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var str = _encoding.GetString(buffer, offset, count);



            await _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var str = _encoding.GetString(buffer, offset, count);
            foreach (var ch in str)
            {
                _lastChars.Enqueue(ch);
                if (_lastChars.Count > _pattern.Length)
                {
                    WriteChar(_lastChars.Dequeue());
                }

                if (_lastChars.Count == _pattern.Length && _lastChars.SequenceEqual(_pattern))
                {
                    _lastChars.Clear();
                    var bytes = _encoding.GetBytes(_replacement);
                    _stream.Write(bytes, 0, bytes.Length);
                }
            }
        }

        private void WriteChar(char ch)
        {
            var bytes = _encoding.GetBytes(new[] { ch });
            _stream.Write(bytes, 0, bytes.Length);
        }
        private async Task WriteCharAsync(char ch)
        {
            var bytes = _encoding.GetBytes(new[] { ch });
            await _stream.WriteAsync(bytes, 0, bytes.Length);
        }


        // Other Stream methods should be implemented as well, here are some of them:

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public override void Flush()
        {

            while (_lastChars.Count > 0)
            {
                WriteChar(_lastChars.Dequeue());
            }
            _stream.Flush();

        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {

            while (_lastChars.Count > 0)
            {
                await WriteCharAsync(_lastChars.Dequeue());
            }
            await _stream.FlushAsync();

        }

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);

        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
    }

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
            if (httpContext.Request.Path == "/" || !matched.Equals(default(KeyValuePair<string, Regex>)))
            {
                var traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;

                _logger.LogInformation("Route matched with NextJS Route: " + matched.Key);
                httpContext.Request.Path = $"{matched.Key ?? "/index.html"}";

                httpContext.Response.Cookies.Append("traceparent", traceId);

              

                // httpContext.Response.Body = new CustomStream(httpContext.Response.Body, Encoding.UTF8, "__REPLACE_TRACEID__", traceId);


                //var originalStream = httpContext.Response.Body;
                //var bufferStream = new MemoryStream();
                //httpContext.Response.Body = bufferStream;
                //await _next(httpContext);

                //bufferStream.Seek(0, SeekOrigin.Begin);

                //var reader = new StreamReader(bufferStream);
                //var response = await reader.ReadToEndAsync();


                //response = response.Replace("__REPLACE_TRACEID__", traceId);

                //// The response string is modified here

                //using var writer = new StreamWriter(originalStream);
                //await writer.WriteAsync(response);
                //await writer.FlushAsync();

            }

            await _next(httpContext);
        }

    }
}
