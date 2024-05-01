using EAVFramework;
using EAVFramework.Endpoints;
using EAVFramework.Shared;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace EAVFW.Extensions.Infrastructure.Crypto
{
    public static class ServiceExtensions
    {
        public static IDataProtectionBuilder PersistKeysToEAVDocuments<TDocument>(this IDataProtectionBuilder builder, string configurationpaht=null)
            where TDocument : DynamicEntity, IXMLRepositoryDocumentEntity, new()
        {
            var o= builder.Services.AddOptions<EAVFWXmlRepositoryOptions>();
            if (!string.IsNullOrEmpty(configurationpaht))
            {
                o.BindConfiguration(configurationpaht);
            }

            builder.Services.AddSingleton<EAVFWXmlRepository<TDocument>>();
            builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>, ConfigureKeyManagementEAVFWOptions<TDocument>>();

           

            return builder;
        }
    }
}
