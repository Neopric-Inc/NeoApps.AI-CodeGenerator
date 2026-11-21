using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Where the app keeps uploaded and generated files (AI documents, job files). Storage:Provider:
    ///   local (default)  files on the API's disk, as before
    ///   azureblob        an Azure Blob container (Storage:Container, default "app-files"). Signs in with the app's managed
    ///                    identity when Storage:BlobAccountUrl is set (https://ACCOUNT.blob.core.windows.net), or with
    ///                    Storage:BlobConnectionString. Container disks are wiped on restart, so serverless apps use this.
    /// A stored file is referenced by its local path, or by "blob:&lt;key&gt;" for a blob; every reader accepts both, so
    /// existing local files keep working.
    /// </summary>
    public static class FileStore
    {
        public const string BlobPrefix = "blob:";
        static BlobContainerClient _container;

        public static bool IsBlob => _container != null;

        public static void Configure(IConfiguration cfg)
        {
            var provider = (cfg["Storage:Provider"] ?? "local").Trim().ToLowerInvariant();
            if (provider == "local" || provider == "") { _container = null; return; }
            if (provider != "azureblob") throw new ArgumentException($"Unknown Storage:Provider '{provider}'. Use local or azureblob.");
            var name = cfg["Storage:Container"] is { Length: > 0 } n ? n : "app-files";
            var cs = cfg["Storage:BlobConnectionString"];
            var url = cfg["Storage:BlobAccountUrl"];
            var service = !string.IsNullOrWhiteSpace(cs) ? new BlobServiceClient(cs)
                : !string.IsNullOrWhiteSpace(url) ? new BlobServiceClient(new Uri(url), new DefaultAzureCredential())
                : throw new ArgumentException("Storage:Provider is azureblob: set Storage:BlobAccountUrl (managed identity) or Storage:BlobConnectionString.");
            _container = service.GetBlobContainerClient(name);
            _container.CreateIfNotExists(PublicAccessType.None);
            Console.WriteLine($"Files: Azure Blob container '{name}'.");
        }

        static string Key(string stored) => stored.Substring(BlobPrefix.Length);
        public static bool IsBlobRef(string stored) => stored != null && stored.StartsWith(BlobPrefix, StringComparison.Ordinal);

        /// <summary>Saves to Blob when configured and returns "blob:key"; returns null when the app stores files locally
        /// (the caller then writes the file the local way).</summary>
        public static string SaveBlob(string key, Stream content, string contentType = null)
        {
            if (!IsBlob) return null;
            key = key.Replace('\\', '/').TrimStart('/');
            _container.GetBlobClient(key).Upload(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType ?? "application/octet-stream" },
            });
            return BlobPrefix + key;
        }

        public static string SaveBlob(string key, byte[] content, string contentType = null)
        {
            using var ms = new MemoryStream(content, writable: false);
            return SaveBlob(key, ms, contentType);
        }

        public static byte[] ReadAllBytes(string stored)
        {
            if (!IsBlobRef(stored)) return File.ReadAllBytes(stored);
            return Container().GetBlobClient(Key(stored)).DownloadContent().Value.Content.ToArray();
        }

        public static Stream OpenRead(string stored) =>
            IsBlobRef(stored) ? Container().GetBlobClient(Key(stored)).OpenRead() : File.OpenRead(stored);

        public static bool Exists(string stored) =>
            IsBlobRef(stored) ? Container().GetBlobClient(Key(stored)).Exists().Value : File.Exists(stored);

        public static void Delete(string stored)
        {
            try
            {
                if (IsBlobRef(stored)) Container().GetBlobClient(Key(stored)).DeleteIfExists();
                else if (File.Exists(stored)) File.Delete(stored);
            }
            catch { /* already gone */ }
        }

        static BlobContainerClient Container() =>
            _container ?? throw new InvalidOperationException("This file is in Azure Blob storage, but Storage:Provider isn't azureblob.");
    }
}
