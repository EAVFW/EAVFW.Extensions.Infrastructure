using EAVFramework;
using EAVFramework.Endpoints;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace EAVFW.Extensions.Infrastructure.Crypto
{
    public class EAVFWXmlRepository<TDocument> : IXmlRepository where TDocument : DynamicEntity, IXMLRepositoryDocumentEntity, new()
    {
        private const int ConflictMaxRetries = 5;
        private static readonly TimeSpan ConflictBackoffPeriod = TimeSpan.FromMilliseconds(200);
        private static readonly XName RepositoryElementName = "repository";
        private static readonly string ContentType = "application/xml; charset=utf-8";


        private readonly Random _random;
        private BlobData _cachedBlobData;
        private readonly IOptions<EAVFWXmlRepositoryOptions> _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public EAVFWXmlRepository(IOptions<EAVFWXmlRepositoryOptions> options, IServiceScopeFactory serviceScopeFactory)
        {
            _options = options;
            _serviceScopeFactory = serviceScopeFactory;
            _random = new Random();
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {


            var data = GetLatestData();

            var doc = CreateDocumentFromBlobData(data);

            return new ReadOnlyCollection<XElement>(doc.Root.Elements().ToList());
        }

        public async void StoreElement(XElement element, string friendlyName)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

          

            // holds the last error in case we need to rethrow it
            ExceptionDispatchInfo lastError = null;

            for (var i = 0; i < ConflictMaxRetries; i++)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EAVDBContext<DynamicContext>>();

                if (i > 1)
                {
                    // If multiple conflicts occurred, wait a small period of time before retrying
                    // the operation so that other writers can make forward progress.
                    Thread.Sleep(GetRandomizedBackoffPeriod());
                }

                if (i > 0)
                {
                    // If at least one conflict occurred, make sure we have an up-to-date
                    // view of the blob contents.
                    GetLatestData();
                }

                // Merge the new element into the document. If no document exists,
                // create a new default document and inject this element into it.

                var latestData = Volatile.Read(ref _cachedBlobData);
                var doc = CreateDocumentFromBlobData(latestData);
                doc.Root.Add(element);

                // Turn this document back into a byte[].

                var serializedDoc = new MemoryStream();
                doc.Save(serializedDoc, SaveOptions.DisableFormatting);
                serializedDoc.Position = 0;

                // Generate the appropriate precondition header based on whether or not
                // we believe data already exists in storage.



                try
                {
                    var dbdoc = new TDocument
                    {
                        Path = "/keys.xml",
                        Container = "protectionkeys",
                        Compressed = false,
                        ContentType = ContentType,
                        Data = serializedDoc.ToArray(),
                    };
                    var claimidentity = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                                   new Claim("sub",_options.Value.Identity)
                                }, EAVFramework.Constants.DefaultCookieAuthenticationScheme));
                    if (latestData != null)
                    {
                        var sameAsCached = db.Set<TDocument>().First(c => c.Container == "protectionkeys" && c.Path == "/keys.xml" && c.RowVersion.Compare(latestData.ETag) == 0);
                        sameAsCached.Data = dbdoc.Data;
                        await db.SaveChangesAsync(claimidentity);
                        dbdoc.RowVersion = sameAsCached.RowVersion;
                    }
                    else
                    {

                        db.Add(dbdoc);
                        await db.SaveChangesAsync(claimidentity);
                    }



                    Volatile.Write(ref _cachedBlobData, new BlobData()
                    {
                        BlobContents = serializedDoc.ToArray(),
                        ETag = dbdoc.RowVersion // was updated by Upload routine
                    });

                    return;
                }
                catch (Exception ex)
                     
                {
                  

                    lastError = ExceptionDispatchInfo.Capture(ex);
                }
            }

            // if we got this far, something went awry
            lastError.Throw();
        }


        private static XDocument CreateDocumentFromBlobData(BlobData blobData)
        {
            if (blobData == null || blobData.BlobContents.Length == 0)
            {
                return new XDocument(new XElement(RepositoryElementName));
            }

            using var memoryStream = new MemoryStream(blobData.BlobContents);

            var xmlReaderSettings = new XmlReaderSettings()
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreProcessingInstructions = true,
            };

            using (var xmlReader = XmlReader.Create(memoryStream, xmlReaderSettings))
            {
                return XDocument.Load(xmlReader);
            }
        }

        private BlobData GetLatestData()
        {
            // Set the appropriate AccessCondition based on what we believe the latest
            // file contents to be, then make the request.

            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EAVDBContext<DynamicContext>>();

            var latestCachedData = Volatile.Read(ref _cachedBlobData); // local ref so field isn't mutated under our feet
                                                                       //var requestCondition = (latestCachedData != null)
                                                                       //    ? new BlobRequestConditions() { IfNoneMatch = latestCachedData.ETag }
                                                                       //    : null;




            var sameAsCached = latestCachedData != null &&
                db.Set<TDocument>()
                .Any(c => c.Container == "protectionkeys" && c.Path == "/keys.xml" && c.RowVersion.Compare(latestCachedData.ETag) == 0);

            if (sameAsCached)
            {
                // 304 Not Modified
                // Thrown when we already have the latest cached data.
                // This isn't an error; we'll return our cached copy of the data.
                return latestCachedData;
            }

            // At this point, our original cache either didn't exist or was outdated.
            // We'll update it now and return the updated value
            latestCachedData = db.Set<TDocument>().Where(c => c.Container == "protectionkeys" && c.Path == "/keys.xml")
            .Select(c => new BlobData { BlobContents = c.Data, ETag = c.RowVersion })
            .FirstOrDefault();

            Volatile.Write(ref _cachedBlobData, latestCachedData);



            return latestCachedData;
        }

        private int GetRandomizedBackoffPeriod()
        {
            // returns a TimeSpan in the range [0.8, 1.0) * ConflictBackoffPeriod
            // not used for crypto purposes
            var multiplier = 0.8 + (_random.NextDouble() * 0.2);
            return (int) (multiplier * ConflictBackoffPeriod.TotalMilliseconds);
        }

        private sealed class BlobData
        {
            internal byte[] BlobContents;
            internal byte[] ETag;
        }
    }
}
