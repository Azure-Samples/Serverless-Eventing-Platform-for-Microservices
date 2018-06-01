using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ContentReactor.Images.Services.Models.Results;
using ContentReactor.Shared;
using ContentReactor.Shared.EventSchemas.Images;
using ContentReactor.Tests.FakeBlobRepository;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Xunit;

namespace ContentReactor.Images.Services.Tests
{
    public class ImagesServiceTests
    {
        #region BeginAddImageNote Tests
        [Fact]
        public void BeginAddImageNote_ReturnsIdAndUrl()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var service = new ImagesService(fakeBlobRepository, new Mock<IImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var output = service.BeginAddImageNote("fakeuserid");

            // assert
            Assert.NotEmpty(output.id);
            Assert.Equal($"https://fakerepository/fullimages/fakeuserid/{output.id}?sasToken=Write, Create", output.url);
        }
        #endregion

        #region CompleteAddImageNote Tests
        [Fact]
        public async Task CompleteAddImageNote_ReturnsSuccess()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid");
            var mockImageValidatorService = new Mock<IImageValidatorService>();
            mockImageValidatorService
                .Setup(m => m.ValidateImage(It.IsAny<Stream>()))
                .Returns(new ValueTuple<bool, string>(true, ""));
            var service = new ImagesService(fakeBlobRepository, mockImageValidatorService.Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var output = await service.CompleteAddImageNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            Assert.Equal(CompleteAddImageNoteResult.Success, output.result);
        }

        [Fact]
        public async Task CompleteAddImageNote_UpdatesBlobMetadata()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid");
            var mockImageValidatorService = new Mock<IImageValidatorService>();
            mockImageValidatorService
                .Setup(m => m.ValidateImage(It.IsAny<Stream>()))
                .Returns(new ValueTuple<bool, string>(true, ""));
            var service = new ImagesService(fakeBlobRepository, mockImageValidatorService.Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.CompleteAddImageNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            Assert.Equal("fakecategory", 
                fakeBlobRepository.Blobs.Single(b => b.ContainerName == ImagesService.FullImagesBlobContainerName).Blob.Metadata[ImagesService.CategoryIdMetadataName]);
            Assert.Equal("fakeuserid", 
                fakeBlobRepository.Blobs.Single(b => b.ContainerName == ImagesService.FullImagesBlobContainerName).Blob.Metadata[ImagesService.UserIdMetadataName]);
        }

        [Fact]
        public async Task CompleteAddImageNote_CreatesAndUploadsPreviewBlob()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid");
            var mockImagePreviewService = new Mock<IImagePreviewService>();
            var mockImageValidatorService = new Mock<IImageValidatorService>();
            mockImageValidatorService
                .Setup(m => m.ValidateImage(It.IsAny<Stream>()))
                .Returns(new ValueTuple<bool, string>(true, ""));
            var service = new ImagesService(fakeBlobRepository, mockImageValidatorService.Object, mockImagePreviewService.Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.CompleteAddImageNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            mockImagePreviewService.Verify(m => 
                    m.CreatePreviewImage(It.IsAny<Stream>()),
                Times.Once);
            Assert.Contains(fakeBlobRepository.Blobs, f => f.ContainerName == ImagesService.PreviewImagesBlobContainerName);
            Assert.Equal("image/jpeg", fakeBlobRepository.Blobs.Single(b => b.ContainerName == ImagesService.PreviewImagesBlobContainerName).ContentType);
        }

        [Fact]
        public async Task CompleteAddImageNote_PublishesImageCreatedEventToEventGrid()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid");
            var mockImageValidatorService = new Mock<IImageValidatorService>();
            mockImageValidatorService
                .Setup(m => m.ValidateImage(It.IsAny<Stream>()))
                .Returns(new ValueTuple<bool, string>(true, ""));
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new ImagesService(fakeBlobRepository, mockImageValidatorService.Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, mockEventGridPublisherService.Object);

            // act
            await service.CompleteAddImageNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            mockEventGridPublisherService.Verify(m => 
                    m.PostEventGridEventAsync(EventTypes.Images.ImageCreated,
                        "fakeuserid/fakeid",
                        It.Is<ImageCreatedEventData>(d => d.Category == "fakecategory")),
                Times.Once);
        }

        [Fact]
        public async Task CompleteAddImageNote_ReturnsImageNotUploaded()
        {
            // arrange
            var mockImageValidatorService = new Mock<IImageValidatorService>();
            mockImageValidatorService
                .Setup(m => m.ValidateImage(It.IsAny<Stream>()))
                .Returns(new ValueTuple<bool, string>(false, ""));
            var service = new ImagesService(new FakeBlobRepository(), mockImageValidatorService.Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var output = await service.CompleteAddImageNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            Assert.Equal(CompleteAddImageNoteResult.ImageNotUploaded, output.result);
        }

        [Fact]
        public async Task CompleteAddImageNote_ReturnsImageTooLarge()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var largeImageStream = new MemoryStream();
            Fill(largeImageStream, 0, ImagesService.MaximumImageSize + 1);

            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid", largeImageStream);
            var mockImageValidatorService = new Mock<IImageValidatorService>();
            mockImageValidatorService
                .Setup(m => m.ValidateImage(It.IsAny<Stream>()))
                .Returns(new ValueTuple<bool, string>(true, ""));
            var service = new ImagesService(fakeBlobRepository, mockImageValidatorService.Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var output = await service.CompleteAddImageNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            Assert.Equal(CompleteAddImageNoteResult.ImageTooLarge, output.result);
        }

        static void Fill(Stream stream, byte value, long count)
        {
            var buffer = new byte[64];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = value;
            }
            while (count > buffer.Length)
            {
                stream.Write(buffer, 0, buffer.Length);
                count -= buffer.Length;
            }
            stream.Write(buffer, 0, (int)count);
        }

        [Fact]
        public async Task CompleteAddImageNote_ReturnsImageAlreadyCreated()
        {
            // arrange
            var blob = new CloudBlockBlob(new Uri("https://fakeblobrepository/fullimages/fakeuserid/fakeid"));
            blob.Metadata.Add(ImagesService.CategoryIdMetadataName, "fakecategory");
            blob.Metadata.Add(ImagesService.UserIdMetadataName, "fakeuserid");
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid", blob);
            var mockImagePreviewService = new Mock<IImagePreviewService>();
            var mockImageValidatorService = new Mock<IImageValidatorService>();
            mockImageValidatorService
                .Setup(m => m.ValidateImage(It.IsAny<Stream>()))
                .Returns(new ValueTuple<bool, string>(true, ""));
            var service = new ImagesService(fakeBlobRepository, mockImageValidatorService.Object, mockImagePreviewService.Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var output = await service.CompleteAddImageNoteAsync("fakeid", "fakeuserid", "fakecategory");
            
            // assert
            Assert.Equal(CompleteAddImageNoteResult.ImageAlreadyCreated, output.result);
        }

        [Fact]
        public async Task CompleteAddImageNote_ReturnsInvalidImage()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid");
            var mockImageValidatorService = new Mock<IImageValidatorService>();
            mockImageValidatorService
                .Setup(m => m.ValidateImage(It.IsAny<Stream>()))
                .Returns(new ValueTuple<bool, string>(false, ""));
            var service = new ImagesService(fakeBlobRepository, mockImageValidatorService.Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var output = await service.CompleteAddImageNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            Assert.Equal(CompleteAddImageNoteResult.InvalidImage, output.result);
        }

        [Fact]
        public async Task CompleteAddImageNote_IncorrectUserId_ReturnsImageNotUploaded()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid2/fakeid");
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid2/fakeid");
            var mockImageValidatorService = new Mock<IImageValidatorService>();
            mockImageValidatorService
                .Setup(m => m.ValidateImage(It.IsAny<Stream>()))
                .Returns(new ValueTuple<bool, string>(false, ""));
            var service = new ImagesService(fakeBlobRepository, mockImageValidatorService.Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var output = await service.CompleteAddImageNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            Assert.Equal(CompleteAddImageNoteResult.ImageNotUploaded, output.result);
        }
        #endregion

        #region GetImageNote Tests
        [Fact]
        public async Task GetImageNote_ReturnsImage()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var fullImageBlob = new CloudBlockBlob(new Uri("https://fakeblobrepository/fullimages/fakeuserid/fakeid"));
            fullImageBlob.Metadata[ImagesService.CaptionMetadataName] = "fakecaption";
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid", fullImageBlob);
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid/fakeid");
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetImageNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal("https://fakeblobrepository/fullimages/fakeuserid/fakeid?sasToken=Read", result.ImageUrl);
            Assert.Equal("https://fakeblobrepository/previewimages/fakeuserid/fakeid?sasToken=Read", result.PreviewUrl);
            Assert.Equal("fakecaption", result.Caption);
        }

        [Fact]
        public async Task GetImageNote_PreviewImageMissing()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid");
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetImageNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal("https://fakeblobrepository/fullimages/fakeuserid/fakeid?sasToken=Read", result.ImageUrl);
            Assert.Null(result.PreviewUrl);
        }

        [Fact]
        public async Task GetImageNote_CaptionMissing()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid");
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid/fakeid");
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetImageNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal("https://fakeblobrepository/fullimages/fakeuserid/fakeid?sasToken=Read", result.ImageUrl);
            Assert.Null(result.Caption);
        }

        [Fact]
        public async Task GetImageNote_InvalidImageId_ReturnsNull()
        {
            // arrange
            var service = new ImagesService(new FakeBlobRepository(), new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetImageNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetImageNote_IncorrectUserId_ReturnsNull()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var fullImageBlob = new CloudBlockBlob(new Uri("https://fakeblobrepository/fullimages/fakeuserid/fakeimageid"));
            fullImageBlob.Metadata[ImagesService.CaptionMetadataName] = "fakecaption";
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid", fullImageBlob);
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid/fakeid");
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetImageNoteAsync("fakeimageid", "fakeuserid");

            // assert
            Assert.Null(result);
        }
        #endregion

        #region ListImageNotes Tests
        [Fact]
        public async Task ListImageNotes_ReturnsIds()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid1", new CloudBlockBlob(new Uri("https://fakeblobrepository/fullimages/fakeuserid/fakeid1")));
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid/fakeid1", new CloudBlockBlob(new Uri("https://fakeblobrepository/previewimages/fakeuserid/fakeid1")));
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid2", new CloudBlockBlob(new Uri("https://fakeblobrepository/fullimages/fakeuserid/fakeid2")));
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid/fakeid2", new CloudBlockBlob(new Uri("https://fakeblobrepository/previewimages/fakeuserid/fakeid2")));
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.ListImageNotesAsync("fakeuserid");

            // assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == "fakeid1");
            Assert.Contains(result, r => r.Id == "fakeid2");
        }

        [Fact]
        public async Task ListImageNotes_DoesNotReturnsIdsForAnotherUser()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid1/fakeid1", new CloudBlockBlob(new Uri("https://fakeblobrepository/fullimages/fakeuserid1/fakeid1")));
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid1/fakeid1", new CloudBlockBlob(new Uri("https://fakeblobrepository/previewimages/fakeuserid1/fakeid1")));
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid2/fakeid2", new CloudBlockBlob(new Uri("https://fakeblobrepository/fullimages/fakeuserid2/fakeid2")));
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid2/fakeid2", new CloudBlockBlob(new Uri("https://fakeblobrepository/previewimages/fakeuserid2/fakeid2")));
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.ListImageNotesAsync("fakeuserid1");

            // assert
            Assert.Single(result);
            Assert.Equal("fakeid1", result.Single().Id);
        }
        #endregion

        #region DeleteImageNote Tests
        [Fact]
        public async Task DeleteImageNote_DeletesBlobs()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid");
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid/fakeid");
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.DeleteImageNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Empty(fakeBlobRepository.Blobs);
        }

        [Fact]
        public async Task DeleteImageNote_PublishesImageDeletedEventToEventGrid()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid");
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid/fakeid");
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, mockEventGridPublisherService.Object);

            // act
            await service.DeleteImageNoteAsync("fakeid", "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                m.PostEventGridEventAsync(EventTypes.Images.ImageDeleted,
                    "fakeuserid/fakeid",
                    It.IsAny<ImageDeletedEventData>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteImageNote_InvalidImageId_ReturnsImageNotFound()
        {
            // arrange
            var service = new ImagesService(new FakeBlobRepository(), new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.DeleteImageNoteAsync("invalidimageid", "fakeuserid");

            // assert
            // no exception thrown
        }

        [Fact]
        public async Task DeleteImageNote_IncorrectUserId_DoesNotDeleteImage()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid2/fakeid");
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid2/fakeid");
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.DeleteImageNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal(2, fakeBlobRepository.Blobs.Count);
        }
        #endregion

        #region UpdateImageCaption Tests
        [Fact]
        public async Task UpdateImageCaption_ReturnsSuccess()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var fullImageBlob = new CloudBlockBlob(new Uri("https://fakeblobrepository/fullimages/fakeuserid/fakeid"));
            fullImageBlob.Metadata[ImagesService.CaptionMetadataName] = "oldcaption";
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid", fullImageBlob);
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid/fakeid");
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateImageNoteCaptionAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal(UpdateImageNoteCaptionResult.Success, result);
        }
        
        [Fact]
        public async Task UpdateImageCaption_UpdatesImageCaption()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var fullImageBlob = new CloudBlockBlob(new Uri("https://fakeblobrepository/fullimages/fakeuserid/fakeid"));
            fullImageBlob.Metadata[ImagesService.CaptionMetadataName] = "oldcaption";
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid", fullImageBlob);
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid/fakeid");
            var mockImageCaptionService = new Mock<IImageCaptionService>();
            mockImageCaptionService
                .Setup(m => m.GetImageCaptionAsync(It.IsAny<byte[]>()))
                .ReturnsAsync("newcaption");
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, mockImageCaptionService.Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.UpdateImageNoteCaptionAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal("newcaption", fakeBlobRepository.Blobs.Single(b => b.ContainerName == ImagesService.FullImagesBlobContainerName).Blob.Metadata[ImagesService.CaptionMetadataName]);
        }
        
        [Fact]
        public async Task UpdateImageCaption_PublishesImageCaptionUpdatedEventToEventGrid()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var fullImageBlob = new CloudBlockBlob(new Uri("https://fakeblobrepository/fullimages/fakeuserid/fakeid"));
            fullImageBlob.Metadata[ImagesService.CaptionMetadataName] = "oldcaption";
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid/fakeid", fullImageBlob);
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid/fakeid");
            var mockImageCaptionService = new Mock<IImageCaptionService>();
            mockImageCaptionService
                .Setup(m => m.GetImageCaptionAsync(It.IsAny<byte[]>()))
                .ReturnsAsync("newcaption");
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, mockImageCaptionService.Object, mockEventGridPublisherService.Object);

            // act
            await service.UpdateImageNoteCaptionAsync("fakeid", "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                m.PostEventGridEventAsync(EventTypes.Images.ImageCaptionUpdated, 
                    "fakeuserid/fakeid",
                    It.Is<ImageCaptionUpdatedEventData>(i => i.Caption == "newcaption")),
                Times.Once);
        }

        [Fact]
        public async Task UpdateImageCaption_ImageNotFound()
        {
            // arrange
            var service = new ImagesService(new FakeBlobRepository(), new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateImageNoteCaptionAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal(UpdateImageNoteCaptionResult.NotFound, result);
        }

        [Fact]
        public async Task UpdateImageCaption_IncorrectUserId_ReturnsNotFound()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var fullImageBlob = new CloudBlockBlob(new Uri("https://fakeblobrepository/fullimages/fakeuserid2/fakeid"));
            fullImageBlob.Metadata[ImagesService.CaptionMetadataName] = "oldcaption";
            fakeBlobRepository.AddFakeBlob(ImagesService.FullImagesBlobContainerName, "fakeuserid2/fakeid", fullImageBlob);
            fakeBlobRepository.AddFakeBlob(ImagesService.PreviewImagesBlobContainerName, "fakeuserid2/fakeid");
            var service = new ImagesService(fakeBlobRepository, new Mock<ImageValidatorService>().Object, new Mock<IImagePreviewService>().Object, new Mock<IImageCaptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateImageNoteCaptionAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal(UpdateImageNoteCaptionResult.NotFound, result);
        }
        #endregion
    }
}
