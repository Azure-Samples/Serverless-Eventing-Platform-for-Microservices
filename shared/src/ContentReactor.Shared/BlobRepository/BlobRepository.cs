using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ContentReactor.Shared.BlobRepository
{
    public interface IBlobRepository
    {
        CloudBlockBlob CreatePlaceholderBlob(string containerName, string folderName, string blobId);
        Task DownloadBlobAsync(CloudBlockBlob blob, Stream stream);
        Task<CloudBlockBlob> UploadBlobAsync(string containerName, string folderName, string blobId, Stream stream, string contentType);
        Task<CloudBlockBlob> GetBlobAsync(string containerName, string folderName, string blobId, bool includeAttributes = false);
        Task<IList<CloudBlockBlob>> ListBlobsInFolderAsync(string containerName, string folderName);
        Task<bool> BlobExistsAsync(CloudBlockBlob blob);
        Task DeleteBlobAsync(string containerName, string folderName, string blobId);
        Task UpdateBlobMetadataAsync(CloudBlockBlob blob);
        string GetSasTokenForBlob(CloudBlockBlob blob, SharedAccessBlobPolicy sasPolicy);
        Task<byte[]> GetBlobBytesAsync(CloudBlockBlob blob);
    }

    public class BlobRepository : IBlobRepository
    {
        private static readonly CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("BlobConnectionString"));
        private static readonly CloudBlobClient BlobClient;

        static BlobRepository()
        {
            // connect to Azure Storage
            BlobClient = StorageAccount.CreateCloudBlobClient();
        }

        public CloudBlockBlob CreatePlaceholderBlob(string containerName, string folderName, string blobId)
        {
            var container = BlobClient.GetContainerReference(containerName);
            var folder = container.GetDirectoryReference(folderName);
            var blob = folder.GetBlockBlobReference(blobId);

            return blob;
        }

        public Task DownloadBlobAsync(CloudBlockBlob blob, Stream stream)
        {
            return blob.DownloadToStreamAsync(stream);
        }
        
        public async Task<CloudBlockBlob> UploadBlobAsync(string containerName, string folderName, string blobId, Stream stream, string contentType)
        {
            var container = BlobClient.GetContainerReference(containerName);
            var folder = container.GetDirectoryReference(folderName);
            var blob = folder.GetBlockBlobReference(blobId);

            // upload blob
            blob.Properties.ContentType = contentType;
            await blob.UploadFromStreamAsync(stream);

            return blob;
        }

        public async Task<CloudBlockBlob> GetBlobAsync(string containerName, string folderName, string blobId, bool includeAttributes = false)
        {
            var container = BlobClient.GetContainerReference(containerName);
            var folder = container.GetDirectoryReference(folderName);
            var blob = folder.GetBlockBlobReference(blobId);

            if (! await blob.ExistsAsync())
            {
                return null;
            }
            if (includeAttributes)
            {
                await blob.FetchAttributesAsync();
            }

            return blob;
        }

        public async Task<IList<CloudBlockBlob>> ListBlobsInFolderAsync(string containerName, string folderName)
        {
            var container = BlobClient.GetContainerReference(containerName);
            var folder = container.GetDirectoryReference(folderName);

            // list all blobs in folder
            var blobsInFolder = new List<CloudBlockBlob>();
            var continuationToken = new BlobContinuationToken();
            do
            {
                var currentPage = await folder.ListBlobsSegmentedAsync(true, BlobListingDetails.Metadata, 100, continuationToken, null, null);
                blobsInFolder.AddRange(currentPage.Results.OfType<CloudBlockBlob>());
                continuationToken = currentPage.ContinuationToken;
            } while (continuationToken != null);

            return blobsInFolder;
        }

        public Task<bool> BlobExistsAsync(CloudBlockBlob blob)
        {
            return blob.ExistsAsync();
        }

        public Task DeleteBlobAsync(string containerName, string folderName, string blobId)
        {
            var container = BlobClient.GetContainerReference(containerName);
            var folder = container.GetDirectoryReference(folderName);
            var blob = folder.GetBlockBlobReference(blobId);
            return blob.DeleteIfExistsAsync();
        }

        public async Task UpdateBlobMetadataAsync(CloudBlockBlob blob)
        {
            var metadataUpdate = blob.SetMetadataAsync();
            var propertiesUpdate = blob.SetPropertiesAsync();
            await Task.WhenAll(metadataUpdate, propertiesUpdate);
        }

        public string GetSasTokenForBlob(CloudBlockBlob blob, SharedAccessBlobPolicy sasPolicy)
        {
            var sasBlobToken = blob.GetSharedAccessSignature(sasPolicy);
            return blob.Uri + sasBlobToken;
        }

        public async Task<byte[]> GetBlobBytesAsync(CloudBlockBlob blob)
        {
            var bytes = new byte[blob.Properties.Length];
            await blob.DownloadToByteArrayAsync(bytes, 0);
            return bytes;
        }
    }
}
