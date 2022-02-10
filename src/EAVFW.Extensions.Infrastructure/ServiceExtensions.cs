using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Extensions.Hosting
{
    public static class ServiceExtensions
    {
        public static bool IsLocal(this IWebHostEnvironment env)
        {
            return env.IsEnvironment("local");
        }
        public static bool IsLocalOrDevelopment(this IWebHostEnvironment env)
        {
            return env.IsDevelopment() || env.IsLocal();
        }
        public static bool IsLocal(this IHostEnvironment env)
        {
            return env.IsEnvironment("local");
        }
        public static bool IsLocalOrDevelopment(this IHostEnvironment env)
        {
            return env.IsDevelopment() || env.IsLocal();
        }
    }
}
