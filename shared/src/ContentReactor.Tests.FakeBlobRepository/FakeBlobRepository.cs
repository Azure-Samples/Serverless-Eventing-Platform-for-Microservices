using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ContentReactor.Shared.BlobRepository;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ContentReactor.Tests.FakeBlobRepository
{
    public class FakeBlobRepository : IBlobRepository
    {
        public class FakeBlobRecord
        {
            public string ContainerName { get; set; }
            public string BlobId { get; set; }
            public CloudBlockBlob Blob { get; set; }
            public string ContentType { get; set; }
            public Stream Stream { get; set; }
        }

        public IList<FakeBlobRecord> Blobs = new List<FakeBlobRecord>();

        public void AddFakeBlob(string containerName, string blobId, Stream stream = null)
        {
            AddFakeBlob(containerName, blobId,
                new CloudBlockBlob(new Uri($"https://fakeblobrepository/{containerName}/{blobId}")), 
                stream);
        }

        public void AddFakeBlob(string containerName, string blobId, CloudBlockBlob blob, Stream stream = null)
        {
            Blobs.Add(new FakeBlobRecord
            {
                BlobId = blobId,
                ContainerName = containerName,
                Blob = blob,
                Stream = stream
            });
        }

        public CloudBlockBlob CreatePlaceholderBlob(string containerName, string folderName, string blobId)
        {
            var uri = new Uri($"https://fakerepository/{containerName}/{folderName}/{blobId}");
            return new CloudBlockBlob(uri);
        }

        public Task DownloadBlobAsync(CloudBlockBlob blob, Stream stream)
        {
            var fakeBlobRecord = Blobs.SingleOrDefault(b => b.Blob == blob);
            if (fakeBlobRecord?.Stream != null)
            {
                if (fakeBlobRecord.Stream.CanSeek)
                {
                    fakeBlobRecord.Stream.Position = 0;
                }

                fakeBlobRecord.Stream.CopyTo(stream);
            }

            return Task.CompletedTask;
        }
        
        public Task<CloudBlockBlob> UploadBlobAsync(string containerName, string folderName, string blobId, Stream stream, string contentType)
        {
            var uri = new Uri($"https://fakerepository/{containerName}/{folderName}/{blobId}");
            var blob = new CloudBlockBlob(uri);

            var fakeBlob = new FakeBlobRecord
            {
                ContainerName = containerName,
                BlobId = blobId,
                ContentType = contentType,
                Blob = blob
            };
            Blobs.Add(fakeBlob);

            return Task.FromResult(blob);
        }

        public Task<CloudBlockBlob> GetBlobAsync(string containerName, string folderName, string blobId, bool includeAttributes = false)
        {
            var fakeBlob = Blobs.SingleOrDefault(i => i.ContainerName == containerName && i.BlobId == $"{folderName}/{blobId}");
            return Task.FromResult(fakeBlob?.Blob);
        }

        public Task<IList<CloudBlockBlob>> ListBlobsInFolderAsync(string containerName, string folderName)
        {
            var blobs = (IList<CloudBlockBlob>)Blobs
                .Where(b => b.ContainerName == containerName && b.BlobId.StartsWith($"{folderName}/"))
                .Select(b => b.Blob)
                .ToList();
            return Task.FromResult(blobs);
        }

        public Task<bool> BlobExistsAsync(CloudBlockBlob blob)
        {
            return Task.FromResult(Blobs.Any(b => b.Blob == blob));
        }

        public Task DeleteBlobAsync(string containerName, string folderName, string blobId)
        {
            var fakeBlob = Blobs.SingleOrDefault(i => i.ContainerName == containerName && i.BlobId == $"{folderName}/{blobId}");
            if (fakeBlob!= null)
            {
                Blobs.Remove(fakeBlob);
            }

            return Task.CompletedTask;
        }

        public Task UpdateBlobMetadataAsync(CloudBlockBlob blob)
        {
            return Task.CompletedTask;
        }

        public string GetSasTokenForBlob(CloudBlockBlob blob)
        {
            var getPolicy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read
            };
            return GetSasTokenForBlob(blob, getPolicy);
        }

        public string GetSasTokenForBlob(CloudBlockBlob blob, SharedAccessBlobPolicy sasPolicy)
        {
            return $"{blob.Uri}?sasToken={sasPolicy.Permissions.ToString()}";
        }

        public Task<byte[]> GetBlobBytesAsync(CloudBlockBlob blob)
        {
            return Task.FromResult(new byte[1]);
        }
    }
}
