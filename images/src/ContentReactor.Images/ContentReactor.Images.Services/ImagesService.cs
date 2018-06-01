using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ContentReactor.Images.Services.Models.Responses;
using ContentReactor.Images.Services.Models.Results;
using ContentReactor.Shared;
using ContentReactor.Shared.BlobRepository;
using ContentReactor.Shared.EventSchemas.Images;
using Microsoft.WindowsAzure.Storage.Blob;
using SixLabors.ImageSharp;

namespace ContentReactor.Images.Services
{
    public interface IImagesService
    {
        (string id, string url) BeginAddImageNote(string userId);
        Task<(CompleteAddImageNoteResult result, string previewUri)> CompleteAddImageNoteAsync(string imageId, string userId, string categoryId);
        Task<ImageNoteDetails> GetImageNoteAsync(string id, string userId);
        Task<ImageNoteSummaries> ListImageNotesAsync(string userId);
        Task DeleteImageNoteAsync(string id, string userId);
        Task<UpdateImageNoteCaptionResult> UpdateImageNoteCaptionAsync(string id, string userId);
    }

    public class ImagesService : IImagesService
    {
        protected IBlobRepository BlobRepository;
        protected IImageValidatorService ImageValidatorService;
        protected IImagePreviewService ImagePreviewService;
        protected IImageCaptionService ImageCaptionService;
        protected IEventGridPublisherService EventGridPublisherService;

        protected internal const string FullImagesBlobContainerName = "fullimages";
        protected internal const string PreviewImagesBlobContainerName = "previewimages";
        protected internal const string CaptionMetadataName = "caption";
        protected internal const string CategoryIdMetadataName = "categoryId";
        protected internal const string UserIdMetadataName = "userId";
        protected internal const long MaximumImageSize = 4L * 1024L * 1024L;
        
        public ImagesService(IBlobRepository blobRepository, IImageValidatorService imageValidatorService, IImagePreviewService imagePreviewService, IImageCaptionService imageCaptionService, IEventGridPublisherService eventGridPublisherService)
        {
            BlobRepository = blobRepository;
            ImageValidatorService = imageValidatorService;
            ImagePreviewService = imagePreviewService;
            ImageCaptionService = imageCaptionService;
            EventGridPublisherService = eventGridPublisherService;
        }

        public (string id, string url) BeginAddImageNote(string userId)
        {
            // generate an ID for this image note
            var imageId = Guid.NewGuid().ToString();

            // create a blob placeholder (which will not have any contents yet)
            var blob = BlobRepository.CreatePlaceholderBlob(FullImagesBlobContainerName, userId, imageId);

            // get a SAS token to allow the client to write the blob
            var writePolicy = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5), // to allow for clock skew
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Write
            };
            var url = BlobRepository.GetSasTokenForBlob(blob, writePolicy);

            return (imageId, url);
        }

        public async Task<(CompleteAddImageNoteResult result, string previewUri)> CompleteAddImageNoteAsync(string imageId, string userId, string categoryId)
        {
            var imageBlob = await BlobRepository.GetBlobAsync(FullImagesBlobContainerName, userId, imageId, true);
            if (imageBlob == null || !await BlobRepository.BlobExistsAsync(imageBlob))
            {
                // the blob hasn't actually been uploaded yet, so we can't process it
                return (CompleteAddImageNoteResult.ImageNotUploaded, null);
            }

            using (var rawImage = new MemoryStream(imageBlob.StreamWriteSizeInBytes))
            {
                // get the image that was uploaded by the client
                await BlobRepository.DownloadBlobAsync(imageBlob, rawImage);
                if (rawImage.CanSeek)
                {
                    rawImage.Position = 0;
                }

                // if the blob already contains metadata then that means it has already been added
                if (imageBlob.Metadata.ContainsKey(CategoryIdMetadataName))
                {
                    return (CompleteAddImageNoteResult.ImageAlreadyCreated, null);
                }

                // validate the size of the image
                if (rawImage.Length > MaximumImageSize) // TODO confirm this works
                {
                    return (CompleteAddImageNoteResult.ImageTooLarge, null);
                }

                // validate the image is in an acceptable format
                var validationResult = ImageValidatorService.ValidateImage(rawImage);
                if (!validationResult.isValid)
                {
                    return (CompleteAddImageNoteResult.InvalidImage, null);
                }
                if (rawImage.CanSeek)
                {
                    rawImage.Position = 0;
                }

                // set the blob metadata
                imageBlob.Metadata.Add(CategoryIdMetadataName, categoryId);
                imageBlob.Metadata.Add(UserIdMetadataName, userId);
                imageBlob.Properties.ContentType = validationResult.mimeType; // the actual detected content type, regardless of what the client may have told us when it uploaded the blob
                await BlobRepository.UpdateBlobMetadataAsync(imageBlob);

                // create and upload a preview image for this blob
                CloudBlockBlob previewImageBlob;
                using (var previewImageStream = ImagePreviewService.CreatePreviewImage(rawImage))
                {
                    previewImageBlob = await BlobRepository.UploadBlobAsync(PreviewImagesBlobContainerName, userId, imageId, previewImageStream, ImageFormats.Jpeg.DefaultMimeType);
                }

                // get a reference to the preview image with a SAS token
                var getPolicy = new SharedAccessBlobPolicy
                {
                    SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5), // to allow for clock skew
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                    Permissions = SharedAccessBlobPermissions.Read
                };
                var previewUrl = BlobRepository.GetSasTokenForBlob(previewImageBlob, getPolicy);
            
                // publish an event into the Event Grid topic
                var eventSubject = $"{userId}/{imageId}";
                await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Images.ImageCreated, eventSubject, new ImageCreatedEventData { PreviewUri = previewUrl, Category = categoryId});

                return (CompleteAddImageNoteResult.Success, previewUrl);
            }
        }

        public async Task<ImageNoteDetails> GetImageNoteAsync(string id, string userId)
        {
            var getPolicy = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5), // to allow for clock skew
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Read
            };

            // get the full-size blob, if it exists
            var imageBlob = await BlobRepository.GetBlobAsync(FullImagesBlobContainerName, userId, id, true);
            if (imageBlob == null)
            {
                return null;
            }
            var imageUrl = BlobRepository.GetSasTokenForBlob(imageBlob, getPolicy);
            imageBlob.Metadata.TryGetValue(CaptionMetadataName, out var caption);

            // get the preview blob, if it exists
            var previewBlob = await BlobRepository.GetBlobAsync(PreviewImagesBlobContainerName, userId, id);
            string previewUrl = null;
            if (previewBlob != null)
            {
                previewUrl = BlobRepository.GetSasTokenForBlob(previewBlob, getPolicy);
            }

            return new ImageNoteDetails
            {
                Id = id,
                ImageUrl = imageUrl,
                PreviewUrl = previewUrl,
                Caption = caption
            };
        }

        public async Task<ImageNoteSummaries> ListImageNotesAsync(string userId)
        {
            var blobs = await BlobRepository.ListBlobsInFolderAsync(FullImagesBlobContainerName, userId);
            var getPolicy = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5), // to allow for clock skew
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Read
            };

            var blobListQueries = blobs
                .Select(async b => new ImageNoteSummary
                {
                    Id = b.Name.Split('/')[1],
                    Preview = BlobRepository.GetSasTokenForBlob(await BlobRepository.GetBlobAsync(PreviewImagesBlobContainerName, userId, b.Name.Split('/')[1]), getPolicy)
                })
                .ToList();
            await Task.WhenAll(blobListQueries);

            var blobList = blobListQueries.Select(q => q.Result).ToList();

            var summaries = new ImageNoteSummaries();
            summaries.AddRange(blobList);
            return summaries;
        }

        public async Task DeleteImageNoteAsync(string id, string userId)
        {
            // delete both image blobs
            var deleteFullImageTask = BlobRepository.DeleteBlobAsync(FullImagesBlobContainerName, userId, id);
            var deletePreviewImageTask = BlobRepository.DeleteBlobAsync(PreviewImagesBlobContainerName, userId, id);
            await Task.WhenAll(deleteFullImageTask, deletePreviewImageTask);

            // fire an event into the Event Grid topic
            var eventSubject = $"{userId}/{id}";
            await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Images.ImageDeleted, eventSubject, new ImageDeletedEventData());
        }

        public async Task<UpdateImageNoteCaptionResult> UpdateImageNoteCaptionAsync(string id, string userId)
        {
            // get the full-size blob, if it exists
            var imageBlob = await BlobRepository.GetBlobAsync(FullImagesBlobContainerName, userId, id, true);
            if (imageBlob == null)
            {
                return UpdateImageNoteCaptionResult.NotFound;
            }

            // get the image bytes
            var bytes = await BlobRepository.GetBlobBytesAsync(imageBlob);
            
            // get the caption
            var caption = await ImageCaptionService.GetImageCaptionAsync(bytes);

            // update the blob with the new caption
            imageBlob.Metadata[CaptionMetadataName] = caption;
            await BlobRepository.UpdateBlobMetadataAsync(imageBlob);

            // fire an event into the Event Grid topic
            var eventSubject = $"{userId}/{id}";
            await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Images.ImageCaptionUpdated, eventSubject, new ImageCaptionUpdatedEventData { Caption = caption });

            return UpdateImageNoteCaptionResult.Success;
        }
    }
}
