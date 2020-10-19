using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using ContentReactor.Images.Services.Models.Responses;
using ContentReactor.Images.Services.Models.Results;
using ContentReactor.Shared;
using ContentReactor.Shared.BlobHelper;
using ContentReactor.Shared.EventSchemas.Images;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        protected BlobHelper BlobHelper;
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

        public ImagesService(BlobHelper blobHelper, IImageValidatorService imageValidatorService, IImagePreviewService imagePreviewService, IImageCaptionService imageCaptionService, IEventGridPublisherService eventGridPublisherService)
        {
            BlobHelper = blobHelper;
            ImageValidatorService = imageValidatorService;
            ImagePreviewService = imagePreviewService;
            ImageCaptionService = imageCaptionService;
            EventGridPublisherService = eventGridPublisherService;
        }

        public (string id, string url) BeginAddImageNote(string userId)
        {
            // generate an ID for this image note
            string imageId = Guid.NewGuid().ToString();

            // create a blob placeholder (which will not have any contents yet)
            BlockBlobClient blob = BlobHelper.GetBlobClient(FullImagesBlobContainerName, imageId);

            string urlAndSas = BlobHelper.GetSasUriForBlob(blob, BlobSasPermissions.Create | BlobSasPermissions.Write);

            return (imageId, urlAndSas);
        }

        public async Task<(CompleteAddImageNoteResult result, string previewUri)> CompleteAddImageNoteAsync(string imageId, string userId, string categoryId)
        {
            BlockBlobClient imageBlob = BlobHelper.GetBlobClient(FullImagesBlobContainerName, imageId);
            if (imageBlob == null || !await imageBlob.ExistsAsync())
            {
                // the blob hasn't actually been uploaded yet, so we can't process it
                return (CompleteAddImageNoteResult.ImageNotUploaded, null);
            }

            using (var rawImage = new MemoryStream())
            {
                // get the image that was uploaded by the client
                await imageBlob.DownloadToAsync(rawImage);
                if (rawImage.CanSeek)
                {
                    rawImage.Position = 0;
                }

                // if the blob already contains metadata then that means it has already been added
                Response<BlobProperties> response = await imageBlob.GetPropertiesAsync();
                if (response.Value.Metadata.ContainsKey(CategoryIdMetadataName))
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
                var metadata = new Dictionary<string, string>
                {
                    {CategoryIdMetadataName, categoryId },
                    { UserIdMetadataName, userId},
                };

                await imageBlob.SetMetadataAsync(metadata);

                // create and upload a preview image for this blob
                BlockBlobClient previewImageBlob;
                using (var previewImageStream = ImagePreviewService.CreatePreviewImage(rawImage))
                {
                    previewImageBlob = BlobHelper.GetBlobClient(PreviewImagesBlobContainerName, imageId);
                    await previewImageBlob.UploadAsync(previewImageStream);
                }

                string previewUrlAndSas = BlobHelper.GetSasUriForBlob(previewImageBlob, BlobSasPermissions.Read);

                // publish an event into the Event Grid topic
                var eventSubject = $"{userId}/{imageId}";
                await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Images.ImageCreated, eventSubject, new ImageCreatedEventData { PreviewUri = previewUrlAndSas, Category = categoryId });

                return (CompleteAddImageNoteResult.Success, previewUrlAndSas);
            }
        }

        public async Task<ImageNoteDetails> GetImageNoteAsync(string id, string userId)
        {
            // get the full-size blob, if it exists
            BlockBlobClient imageBlob = BlobHelper.GetBlobClient(FullImagesBlobContainerName, id);
            if (imageBlob == null)
            {
                return null;
            }

            string imageUrl = BlobHelper.GetSasUriForBlob(imageBlob, BlobSasPermissions.Read);

            Response<BlobProperties> response = await imageBlob.GetPropertiesAsync();
            response.Value.Metadata.TryGetValue(CaptionMetadataName, out var caption);

            // get the preview blob, if it exists
            BlockBlobClient previewBlob = BlobHelper.GetBlobClient(PreviewImagesBlobContainerName, id);
            string previewUrl = null;
            if (previewBlob != null)
            {
                previewUrl = BlobHelper.GetSasUriForBlob(previewBlob, BlobSasPermissions.Read);
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
            IList<BlobItem> blobs = await BlobHelper.ListBlobsAsync(FullImagesBlobContainerName);

            var blobListQueries = blobs
                .Select(b => new ImageNoteSummary
                {
                    Id = b.Name.Split('/')[1],
                    Preview = BlobHelper.GetSasUriForBlob(BlobHelper.GetBlobClient(PreviewImagesBlobContainerName, b.Name.Split('/')[1]), BlobSasPermissions.Read)
                })
                .ToList();

            var summaries = new ImageNoteSummaries();
            summaries.AddRange(blobListQueries);
            return summaries;
        }

        public async Task DeleteImageNoteAsync(string id, string userId)
        {
            // delete both image blobs
            BlockBlobClient deleteFullImageBlob = BlobHelper.GetBlobClient(FullImagesBlobContainerName, id);
            await deleteFullImageBlob.DeleteIfExistsAsync();

            BlockBlobClient deletePreviewImageBlob = BlobHelper.GetBlobClient(PreviewImagesBlobContainerName, id);
            await deletePreviewImageBlob.DeleteIfExistsAsync();

            // fire an event into the Event Grid topic
            var eventSubject = $"{userId}/{id}";
            await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Images.ImageDeleted, eventSubject, new ImageDeletedEventData());
        }

        public async Task<UpdateImageNoteCaptionResult> UpdateImageNoteCaptionAsync(string id, string userId)
        {
            // get the full-size blob, if it exists
            BlockBlobClient imageBlob = BlobHelper.GetBlobClient(FullImagesBlobContainerName, id);
            if (imageBlob == null)
            {
                return UpdateImageNoteCaptionResult.NotFound;
            }

            // get the image bytes
            BlobDownloadInfo downloadInfo = await imageBlob.DownloadAsync();
            var memoryStream = new MemoryStream();
            await downloadInfo.Content.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            // get the caption
            string caption = await ImageCaptionService.GetImageCaptionAsync(bytes);

            // update the blob with the new caption
            var metadata = new Dictionary<string, string> { { CaptionMetadataName, caption } };
            await imageBlob.SetMetadataAsync(metadata);

            // fire an event into the Event Grid topic
            var eventSubject = $"{userId}/{id}";
            await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Images.ImageCaptionUpdated, eventSubject, new ImageCaptionUpdatedEventData { Caption = caption });

            return UpdateImageNoteCaptionResult.Success;
        }
    }
}
