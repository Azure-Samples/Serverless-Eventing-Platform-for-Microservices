using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ContentReactor.Shared.BlobRepository
{
    public interface IBlobRepository
    {
        BlockBlobClient CreatePlaceholderBlob(string containerName, string blobId);
        Task DownloadBlobAsync(BlockBlobClient blob, Stream stream);
        Task<BlockBlobClient> UploadBlobAsync(string containerName, string blobId, Stream stream);
        Task<BlockBlobClient> GetBlobAsync(string containerName, string blobId);
        Task<IList<BlobItem>> ListBlobsAsync(string containerName);
        Task<Response<bool>> BlobExistsAsync(BlockBlobClient blob);
        Task DeleteBlobAsync(string containerName, string blobId);
        Task UpdateBlobMetadataAsync(BlockBlobClient blob, IDictionary<string, string> metaData);
        string GetSasTokenForBlob(BlockBlobClient blob, BlobSasBuilder sasBuilder);
        Task<byte[]> GetBlobBytesAsync(BlockBlobClient blob);
    }

    public class BlobRepository : IBlobRepository
    {
        private static readonly BlobServiceClient BlobServiceClient;

        static BlobRepository()
        {
            // connect to Azure Storage
            string serviceUri = Environment.GetEnvironmentVariable("STORAGE_BLOB_SERVICE_URI");
            var credential = new StorageSharedKeyCredential(Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME"), Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_KEY"));
            BlobServiceClient = new BlobServiceClient(new Uri(serviceUri), credential);
        }

        public BlockBlobClient CreatePlaceholderBlob(string containerName, string blobId)
        {
            var container = BlobServiceClient.GetBlobContainerClient(containerName);
            container.CreateIfNotExists();
            var blob = container.GetBlockBlobClient(blobId);

            return blob;
        }

        public async Task DownloadBlobAsync(BlockBlobClient blob, Stream stream)
        {
            await blob.DownloadToAsync(stream);
        }

        public async Task<BlockBlobClient> UploadBlobAsync(string containerName, string blobId, Stream stream)
        {
            var container = BlobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobClient(blobId);

            // upload blob
            await blob.UploadAsync(stream);

            return blob;
        }

        public async Task<BlockBlobClient> GetBlobAsync(string containerName, string blobId)
        {
            var container = BlobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobClient(blobId);

            if (!await blob.ExistsAsync())
            {
                return null;
            }

            return blob;
        }

        public async Task<IList<BlobItem>> ListBlobsAsync(string containerName)
        {
            var container = BlobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync();

            var blobs = new List<BlobItem>();
            await foreach (var blob in container.GetBlobsAsync())
            {
                blobs.Add(blob);
            }

            return blobs;
        }

        public Task<Response<bool>> BlobExistsAsync(BlockBlobClient blob)
        {
            return blob.ExistsAsync();
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            var container = BlobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobClient(blobName);

            await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }

        public async Task UpdateBlobMetadataAsync(BlockBlobClient blob, IDictionary<string, string> metaData)
        {
            await blob.SetMetadataAsync(metaData);
        }

        public string GetSasTokenForBlob(BlockBlobClient blob, BlobSasBuilder sasBuilder)
        {
            // Create a SharedKeyCredential that we can use to sign the SAS token
            var credential = new StorageSharedKeyCredential(Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_NAME"), Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_KEY"));

            // Build a SAS URI
            UriBuilder sasUri = new UriBuilder(blob.Uri)
            {
                Query = sasBuilder.ToSasQueryParameters(credential).ToString()
            };

            return sasUri.Uri.ToString();
        }

        // Currently Azure.Storage.Blob doesn't has DownloadToByteArrayAsync() API, remove comments once API released.
        public async Task<byte[]> GetBlobBytesAsync(BlockBlobClient blob)
        {
            BlobDownloadInfo downloadInfo = await blob.DownloadAsync();
            using var memoryStream = new MemoryStream();
            await downloadInfo.Content.CopyToAsync(memoryStream);

            return memoryStream.ToArray();
        }
    }
}
