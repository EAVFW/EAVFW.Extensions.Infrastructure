using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EAVFW.Extensions.Infrastructure.TypeHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.ModelBuilder.Config;

[assembly: AssemblyAutoLoad()]

namespace EAVFW.Extensions.Infrastructure.TypeHelpers
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class AssemblyAutoLoadAttribute : Attribute
    {
       
    }

    public interface IApplicationServiceRegistration
    {
        public void ConfigureServices(IServiceCollection services);
    }

    public static class FindTypes
    {
        public static IEnumerable<Type> FindAllTypes()
        {
            return Assembly
                .GetEntryAssembly().DefinedTypes.Concat(Assembly
                .GetEntryAssembly()
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Where(a => a.GetCustomAttributes<AssemblyAutoLoadAttribute>().Any())
                .SelectMany(x => x.DefinedTypes));
                
        }

        public static IEnumerable<T> FindAllImplementing<T>()
        {
            return FindAllTypes().Where(type => typeof(IApplicationServiceRegistration).IsAssignableFrom(type))
                .Select(Activator.CreateInstance).Cast<T>();
        }

        public static IServiceCollection AddAssemblyDiscovery(this IServiceCollection services)
        {
            foreach(var service in FindAllImplementing<IApplicationServiceRegistration>())
            {
                service.ConfigureServices(services);
            }

            return services;
        }
    }

    public class EAVDefaultServices : IApplicationServiceRegistration
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddHealthChecks();
            services.AddCors();

            services.ConfigureForwardedHeadersOptions();
             
        }
    }
}
