using EAVFramework;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace EAVFW.Extensions.Infrastructure.Crypto
{
    public class ConfigureKeyManagementEAVFWOptions<TDocument> : IConfigureOptions<KeyManagementOptions> where TDocument : DynamicEntity, IXMLRepositoryDocumentEntity, new()
    {
        private readonly EAVFWXmlRepository<TDocument> _xmlRepository;

        public ConfigureKeyManagementEAVFWOptions(EAVFWXmlRepository<TDocument> xmlRepository)
        {
            _xmlRepository = xmlRepository;
        }

        public void Configure(KeyManagementOptions options)
        {
            options.XmlRepository = _xmlRepository;
        }
    }
}
