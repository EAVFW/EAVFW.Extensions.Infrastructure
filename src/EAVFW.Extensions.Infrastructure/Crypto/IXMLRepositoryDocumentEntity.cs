using EAVFramework.Shared;

namespace EAVFW.Extensions.Infrastructure.Crypto
{
    [EntityInterface(EntityKey = "Document")]
    public interface IXMLRepositoryDocumentEntity
    {
        string Container { get; set; }
        string Path { get; set; }
        byte[] Data { get; set; }
        bool? Compressed { get; set; }
        string ContentType { get; set; }
        byte[] RowVersion { get; set; }
    }
}
